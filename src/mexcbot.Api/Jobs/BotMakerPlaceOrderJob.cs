using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.Models.Mexc;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Extensions;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class BotMakerPlaceOrderJob : BackgroundService
    {
        public BotMakerPlaceOrderJob()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = CreateOrderJob(stoppingToken);

            await Task.WhenAll(task);
        }

        private async Task CreateOrderJob(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                    await dbConnection.OpenAsync();

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND (NextRunMakerTime < @Now OR NextRunMakerTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.MAKER,
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    if (bots.Count == 0)
                        continue;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotMakerPlaceOrderJob:CreateOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task Run(BotDto bot)
        {
            var stopLog = "";
            var now = AppUtils.NowMilis();

            try
            {
                Log.Information("BOT {0} run", bot.Symbol);

                ExchangeClient client = bot.ExchangeType switch
                {
                    BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey,
                        bot.ApiSecret),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
                var selfSymbols = await client.GetSelfSymbols();
                var bot24hr = (await client.GetTicker24hr(bot.Base, bot.Quote));
                var balances = await client.GetAccInformation();

                #region Update info bot

                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                    await dbConnection.OpenAsync();

                    var accInfo = new AccInfo
                    {
                        Balances = balances
                    };

                    bot.ExchangeInfo = (exchangeInfo == null || string.IsNullOrEmpty(exchangeInfo.Symbol))
                        ? string.Empty
                        : JsonConvert.SerializeObject(exchangeInfo);
                    bot.AccountInfo = !balances.Any() ? string.Empty : JsonConvert.SerializeObject(accInfo);

                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET ExchangeInfo = @ExchangeInfo, AccountInfo = @AccountInfo
                    WHERE Id = @Id",
                        bot);
                }
                catch (Exception e)
                {
                    Log.Error(e, "BotMakerPlaceOrderJob: Update Bots {Id} {@data1} {@data2}", bot.Id, bot.ExchangeInfo,
                        bot.AccountInfo);
                }

                #endregion

                var makerOption = new BotMakerOption();
                var lastBtcPrice = 0m;

                #region Validate

                var baseBalanceValue = 0m;
                var quoteBalanceValue = 0m;

                if (!selfSymbols.Contains(bot.Symbol))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"{bot.Symbol} is not support; \n";
                }

                if (exchangeInfo == null || string.IsNullOrEmpty(exchangeInfo.Symbol))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when exchange info not found; \n";
                }

                if (string.IsNullOrEmpty(bot.MakerOption))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when maker option is null; \n";
                }
                else
                {
                    makerOption = JsonConvert.DeserializeObject<BotMakerOption>(bot.MakerOption);

                    bot.NextRunMakerTime =
                        now + (int)RandomNumber(makerOption.MinInterval, makerOption.MaxInterval, 0) * 1000;

                    if (makerOption.IsFollowBtc)
                    {
                        if (makerOption.FollowBtcBasePrice <= 0
                            || makerOption.FollowBtcBtcPrice <= 0)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += "Follow BTC price settings wrong; \n";
                        }

                        var lastBtcPriceStr = (await client.GetTicker24hr("BTC", "USDT")).LastPrice;

                        if (decimal.TryParse(lastBtcPriceStr, out var btcPrice))
                            lastBtcPrice = btcPrice;

                        if (lastBtcPrice <= 0)
                        {
                            Log.Error("BOT {0} get BTC price error", bot.Symbol);
                            return;
                        }
                    }

                    if (bot24hr == null || string.IsNullOrEmpty(bot24hr.Symbol))
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += "Bot get 24hr fail; \n";
                    }
                    else
                    {
                        var botLastPrice = decimal.Parse(bot24hr.LastPrice);

                        if (botLastPrice == 0)
                            return;

                        if (makerOption.MinStopPrice < 0 && botLastPrice <= makerOption.MinStopPrice)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when price cross down {makerOption.MinStopPrice}; \n";
                        }

                        if (makerOption.MaxStopPrice > 0 && botLastPrice >= makerOption.MaxStopPrice)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when price cross up {makerOption.MaxStopPrice}; \n";
                        }
                    }

                    if (!balances.Any())
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += "Stop when your balances Zero; \n";
                    }
                    else
                    {
                        if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.SELL)
                        {
                            var baseBalance = balances.FirstOrDefault(x => x.Asset == bot.Base);

                            if (baseBalance == null)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Base} balance below 0 or null; \n";
                            }
                            else
                            {
                                if (decimal.TryParse(baseBalance.Free, out var value))
                                    baseBalanceValue = value;

                                if (baseBalanceValue <= 0)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog += $"Stop when your {bot.Base} balance below 0 or null; \n";
                                }
                                else
                                {
                                    if (makerOption.StopLossBase > 0)
                                    {
                                        if (baseBalanceValue <= makerOption.StopLossBase)
                                        {
                                            bot.Status = BotStatus.INACTIVE;
                                            stopLog +=
                                                $"Stop when your {bot.Base} balance lower than {makerOption.StopLossBase}; \n";
                                        }
                                    }
                                }
                            }
                        }

                        if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.BUY)
                        {
                            var quoteBalance = balances.FirstOrDefault(x => x.Asset == bot.Quote);

                            if (quoteBalance == null)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Quote} balance below 0 or null; \n";
                            }
                            else
                            {
                                if (decimal.TryParse(quoteBalance.Free, out var value))
                                    quoteBalanceValue = value;

                                if (quoteBalanceValue <= 0)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog += $"Stop when your {bot.Quote} balance below 0 or null; \n";
                                }
                                else
                                {
                                    if (makerOption.StopLossQuote > 0)
                                    {
                                        if (quoteBalanceValue <= makerOption.StopLossQuote)
                                        {
                                            bot.Status = BotStatus.INACTIVE;
                                            stopLog +=
                                                $"Stop when your {bot.Quote} balance lower than {makerOption.StopLossQuote}; \n";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (exchangeInfo != null && bot24hr != null && !string.IsNullOrEmpty(exchangeInfo.Symbol) &&
                    !string.IsNullOrEmpty(bot24hr.Symbol))
                {
                    var minQty = (decimal.Parse(exchangeInfo.QuoteAmountPrecision) /
                                  decimal.Parse(bot24hr.LastPrice));

                    if (makerOption.MinQty < minQty)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog +=
                            $"Min quantity must be bigger than required quantity {minQty}; \n";
                    }
                }

                //Stop
                if (bot.Status == BotStatus.INACTIVE)
                {
                    bot.Logs = stopLog;
                    await UpdateBot(bot);
                    return;
                }
                else
                {
                    await UpdateBot(bot, false);
                }

                #endregion

                if (!string.IsNullOrEmpty(bot.MakerOption))
                {
                    var price = 0m;

                    var quotePrecision = exchangeInfo.QuoteAssetPrecision;
                    var basePrecision = exchangeInfo.BaseAssetPrecision;

                    var orderbook = (await client.GetOrderbook(bot.Base, bot.Quote));

                    if (orderbook == null || orderbook.Asks.Count == 0 || orderbook.Asks.Count == 0)
                    {
                        Log.Error("Order not found");
                        return;
                    }

                    const decimal spreadHighPercent = 5;
                    const decimal spreadFixPercent = 0.5m;

                    if (orderbook.Asks.Count == 0 || orderbook.Bids.Count == 0)
                    {
                        Log.Error("Order not found");
                        return;
                    }

                    var maxPrice = orderbook.Asks.Min(x => x[0]);
                    var minPrice = orderbook.Bids.Max(x => x[0]);
                    var spreadHigh = Math.Abs((minPrice - maxPrice) * 100 / minPrice) > spreadHighPercent;

                    try
                    {
                        var numOfTrades =
                            (int)RandomNumber(makerOption.MinTradePerExec, makerOption.MaxTradePerExec, 0);

                        var tasks = new List<Task>();

                        for (var i = 0; i < numOfTrades; i++)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                #region Price

                                if (makerOption.IsFollowBtc)
                                {
                                    var change = 100 * (lastBtcPrice - makerOption.FollowBtcBtcPrice) /
                                                 makerOption.FollowBtcBtcPrice;

                                    if (makerOption.FollowBtcRate > 0)
                                        change = change * makerOption.FollowBtcRate;

                                    price = RandomNumber(
                                        makerOption.FollowBtcBasePrice + makerOption.FollowBtcBasePrice *
                                        (change + makerOption.MinPriceStep) / 100,
                                        makerOption.FollowBtcBasePrice + makerOption.FollowBtcBasePrice *
                                        (change + makerOption.MaxPriceStep) / 100, quotePrecision);
                                }
                                else
                                {
                                    Log.Warning("BOT {0} is not support for option", bot.Symbol);
                                    return;
                                }

                                if (price <= 0)
                                {
                                    Log.Warning("BOT {0} price=0", bot.Symbol);
                                    return;
                                }

                                #endregion

                                #region Qty

                                decimal qty;

                                if (makerOption.IsRandomQty)
                                {
                                    qty = RandomNumber(makerOption.MinQty, makerOption.MinQty * 2, basePrecision);
                                }
                                else
                                {
                                    qty = RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision);
                                }

                                #endregion

                                #region Trade

                                price = price.Truncate(quotePrecision);
                                qty = qty.Truncate(basePrecision);

                                var total = Math.Round(price * qty, 8);

                                if (makerOption.Side == OrderSide.BUY && quoteBalanceValue > total)
                                {
                                    await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                        price.ToString($"F{quotePrecision.ToString()}"), OrderSide.BUY,false);
                                }
                                else if (makerOption.Side == OrderSide.SELL && baseBalanceValue > qty)
                                {
                                    await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                        price.ToString($"F{quotePrecision.ToString()}"), OrderSide.SELL);
                                }
                                else if (makerOption.Side == OrderSide.BOTH && baseBalanceValue > qty &&
                                         quoteBalanceValue > total)
                                {
                                    if (makerOption.MinMatchingTime == 0 &&
                                        makerOption.MaxMatchingTime == 0)
                                    {
                                        if (await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                                price.ToString($"F{quotePrecision.ToString()}"), OrderSide.SELL))
                                        {
                                            await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                                price.ToString($"F{quotePrecision.ToString()}"), OrderSide.BUY);
                                        }
                                    }
                                    else
                                    {
                                        if (await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                                price.ToString($"F{quotePrecision.ToString()}"), OrderSide.SELL))
                                        {
                                            await TradeDelay(bot);

                                            await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                                price.ToString($"F{quotePrecision.ToString()}"), OrderSide.BUY);
                                        }
                                    }
                                }

                                #endregion

                                #region Order Over Step

                                var overStepPrice = 0m;

                                if (makerOption.MinPriceOverStep < 0)
                                {
                                    if (price > 0)
                                    {
                                        overStepPrice = RandomNumber(
                                            price + (makerOption.MinPriceStep + makerOption.MinPriceOverStep) * price /
                                            100,
                                            price + makerOption.MinPriceStep * price / 100, quotePrecision);
                                    }

                                    if (overStepPrice > 0)
                                        await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                            overStepPrice.ToString($"F{quotePrecision.ToString()}"), OrderSide.BUY, true);
                                }

                                if (makerOption.MaxPriceOverStep > 0)
                                {
                                    if (price > 0)
                                    {
                                        overStepPrice = RandomNumber(
                                            price + makerOption.MaxPriceOverStep *
                                            price / 100,
                                            price + (makerOption.MaxPriceStep + makerOption.MaxPriceOverStep) *
                                            price / 100, quotePrecision);
                                    }

                                    if (overStepPrice > 0)
                                        await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                            overStepPrice.ToString($"F{quotePrecision.ToString()}"), OrderSide.SELL, true);
                                }

                                #endregion

                                #region BTC Spread

                                if (makerOption.IsFollowBtc && spreadHigh)
                                {
                                    //Buy more 
                                    if (price >= maxPrice)
                                    {
                                        var buyPrice = minPrice * (1 + spreadFixPercent / 100);
                                        buyPrice = buyPrice.Truncate(quotePrecision);
                                        await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                            buyPrice.ToString($"F{quotePrecision.ToString()}"), OrderSide.BUY);
                                    }
                                    //Sell more 
                                    else if (price <= minPrice)
                                    {
                                        var sellPrice = maxPrice * (1 - spreadFixPercent / 100);
                                        sellPrice = sellPrice.Truncate(quotePrecision);
                                        await CreateLimitOrder(client, bot, qty.ToString($"F{basePrecision.ToString()}"),
                                            sellPrice.ToString($"F{quotePrecision.ToString()}"), OrderSide.SELL);
                                    }
                                }

                                #endregion
                            }, CancellationToken.None));
                        }

                        await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Bot trade");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Bot run");
            }
        }

        #region Private

        private async Task<bool> CreateLimitOrder(ExchangeClient client, BotDto bot, string qty, string price,
            OrderSide side, bool isOverStepOrder = false)
        {
            price = bot.ExchangeType == BotExchangeType.LBANK ? price.Replace(",", ".") : price;
            qty = bot.ExchangeType == BotExchangeType.LBANK ? qty.Replace(",", ".") : price;

            var order = await client.PlaceOrder(bot.Base, bot.Quote, side, qty,
                price);

            if (order == null)
                return false;

            if (string.IsNullOrEmpty(order.OrderId))
                return false;

            Log.Information("Bot create order {0}",
                $"{side} {qty} {bot.Symbol} at price {price} {order.OrderId}");

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

            order.BotId = bot.Id;
            order.BotType = bot.Type;
            order.BotExchangeType = bot.ExchangeType;
            order.UserId = bot.UserId;

            if (isOverStepOrder && bot.MakerOptionObj != null && bot.MakerOptionObj.OrderExp > 0)
                order.ExpiredTime = order.TransactTime + (bot.MakerOptionObj.OrderExp * 1000);
            else
                order.ExpiredTime = order.TransactTime + MexcBotConstants.ExpiredOrderTime;

            var exec = await sqlConnection.ExecuteAsync(
                @"INSERT INTO BotOrders(BotId,BotType,BotExchangeType,UserId,OrderId,Symbol,OrderListId,Price,OrigQty,Type,Side,ExpiredTime,Status,`TransactTime`)
                      VALUES(@BotId,@BotType,@BotExchangeType,@UserId,@OrderId,@Symbol,@OrderListId,@Price,@OrigQty,@Type,@Side,@ExpiredTime,@Status,@TransactTime)",
                order);

            if (exec == 0)
                Log.Error("Bot insert order fail {@data}", order);

            return true;
        }

        private async Task UpdateBot(BotDto bot, bool isInactive = true)
        {
            try
            {
                await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                await dbConnection.OpenAsync();

                if (isInactive)
                {
                    bot.NextRunMakerTime = AppUtils.NowMilis() + (long)TimeSpan.FromMinutes(1).TotalMilliseconds;

                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET Logs = @Logs, Status = @Status, NextRunMakerTime = @NextRunMakerTime
                    WHERE Id = @Id",
                        bot);
                }
                else
                {
                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET NextRunMakerTime = @NextRunMakerTime
                    WHERE Id = @Id",
                        bot);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateBot");
            }
        }

        private async Task TradeDelay(BotDto bot)
        {
            var volumeOption = JsonConvert.DeserializeObject<BotMakerOption>(bot.MakerOption);

            await Task.Delay((int)RandomNumber(volumeOption.MinMatchingTime,
                volumeOption.MaxMatchingTime, 1) * 1000);
        }

        private decimal RandomNumber(decimal from, decimal to, int precision)
        {
            if (from >= to)
                return from;

            var roundPrecision = (int)Math.Pow(10, precision);

            return (decimal)new Random().Next((int)(from * roundPrecision), (int)(to * roundPrecision)) /
                   roundPrecision;
        }

        #endregion
    }
}