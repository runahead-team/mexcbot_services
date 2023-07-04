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
            var ver = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                    await dbConnection.OpenAsync();

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type", new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.MAKER
                        })).ToList();

                    if (!bots.Any())
                        return;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);

                    ver++;
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotMakerPlaceOrderJob:CreateOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
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

                var mexcClient = new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret);

                var exchangeInfo = await mexcClient.GetExchangeInfo(bot.Base, bot.Quote);
                var bot24hr = (await mexcClient.GetTicker24hr(bot.Base, bot.Quote));

                var makerOption = new BotMakerOption();
                var lastBtcPrice = 0m;

                #region Validate

                var balances = await mexcClient.GetAccInformation();

                var baseBalanceValue = 0m;
                var quoteBalanceValue = 0m;

                if (exchangeInfo == null)
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when exchange info not found\n";
                }

                if (string.IsNullOrEmpty(bot.MakerOption))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when maker option is null\n";
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
                            stopLog += "Follow BTC price settings wrong\n";
                        }

                        var lastBtcPriceStr = (await mexcClient.GetTicker24hr("BTC", "USDT")).LastPrice;

                        if (decimal.TryParse(lastBtcPriceStr, out var btcPrice))
                            lastBtcPrice = btcPrice;

                        if (lastBtcPrice <= 0)
                        {
                            Log.Error("BOT {0} get BTC price error", bot.Symbol);
                            return;
                        }
                    }

                    if (bot24hr == null)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += "Bot get 24hr fail\n";
                    }
                    else
                    {
                        var botLastPrice = decimal.Parse(bot24hr.LastPrice);

                        if (botLastPrice == 0)
                            return;

                        if (makerOption.MinStopPrice < 0 && botLastPrice <= makerOption.MinStopPrice)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when price cross down {makerOption.MinStopPrice}\n";
                        }

                        if (makerOption.MaxStopPrice > 0 && botLastPrice >= makerOption.MaxStopPrice)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when price cross up {makerOption.MaxStopPrice}\n";
                        }
                    }

                    if (!balances.Any())
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += "Stop when your balances Zero\n";
                    }
                    else
                    {
                        var baseBalance = balances.FirstOrDefault(x => x.Asset == bot.Base);

                        if (baseBalance == null)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when your {bot.Base} balance below 0 or null\n";
                        }
                        else
                        {
                            if (decimal.TryParse(baseBalance.Free, out var value))
                                baseBalanceValue = value;

                            if (baseBalanceValue <= 0)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Base} balance below 0 or null\n";
                            }
                            else
                            {
                                if (makerOption.StopLossBase > 0)
                                {
                                    if (baseBalanceValue <= makerOption.StopLossBase)
                                    {
                                        bot.Status = BotStatus.INACTIVE;
                                        stopLog +=
                                            $"Stop when your {bot.Base} balance lower than {makerOption.StopLossBase}\n";
                                    }
                                }
                            }
                        }

                        var quoteBalance = balances.FirstOrDefault(x => x.Asset == bot.Quote);

                        if (quoteBalance == null)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when your {bot.Quote} balance below 0 or null\n";
                        }
                        else
                        {
                            if (decimal.TryParse(quoteBalance.Free, out var value))
                                quoteBalanceValue = value;

                            if (quoteBalanceValue <= 0)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Quote} balance below 0 or null\n";
                            }
                            else
                            {
                                if (makerOption.StopLossQuote > 0)
                                {
                                    if (quoteBalanceValue <= makerOption.StopLossQuote)
                                    {
                                        bot.Status = BotStatus.INACTIVE;
                                        stopLog +=
                                            $"Stop when your {bot.Quote} balance lower than {makerOption.StopLossQuote}\n";
                                    }
                                }
                            }
                        }
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

                    var orderbook = (await mexcClient.GetOrderbook(bot.Base, bot.Quote));

                    if (orderbook == null || orderbook.Asks.Count == 0 || orderbook.Asks.Count == 0)
                        return;

                    const decimal spreadHighPercent = 2;
                    const decimal spreadFixPercent = 0.5m;

                    if (orderbook.Asks.Count == 0 || orderbook.Bids.Count == 0)
                        return;

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

                                    change = change * 0.5m;

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
                                    await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                        price.ToString($"F{quotePrecision}"), OrderSide.BUY);
                                }
                                else if (makerOption.Side == OrderSide.SELL && baseBalanceValue > qty)
                                {
                                    await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                        price.ToString($"F{quotePrecision}"), OrderSide.SELL);
                                }
                                else if (makerOption.Side == OrderSide.BOTH && baseBalanceValue > qty &&
                                         quoteBalanceValue > total)
                                {
                                    if (makerOption.MinMatchingTime == 0 &&
                                        makerOption.MaxMatchingTime == 0)
                                    {
                                        if (await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                                price.ToString($"F{quotePrecision}"), OrderSide.SELL))
                                        {
                                            await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                                price.ToString($"F{quotePrecision}"), OrderSide.BUY);
                                        }
                                    }
                                    else
                                    {
                                        if (await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                                price.ToString($"F{quotePrecision}"), OrderSide.SELL))
                                        {
                                            await TradeDelay(bot);

                                            await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                                price.ToString($"F{quotePrecision}"), OrderSide.BUY);
                                        }
                                    }
                                }

                                #endregion

                                // #region Order Over Step
                                //
                                // if (makerOption.MinPriceOverStep < 0)
                                // {
                                //     if (makerOption.LastPrice)
                                //     {
                                //         price = RandomNumber(
                                //             bot.LastPrice + (makerOption.MinPriceStep + makerOption.MinPriceOverStep) *
                                //             bot.LastPrice / 100,
                                //             bot.LastPrice + makerOption.MinPriceStep * bot.LastPrice / 100);
                                //     }
                                //     else if (makerOption.BasePrice > 0)
                                //     {
                                //         price = RandomNumber(
                                //             makerOption.BasePrice +
                                //             (makerOption.MinPriceStep + makerOption.MinPriceOverStep) *
                                //             makerOption.BasePrice / 100,
                                //             makerOption.BasePrice +
                                //             makerOption.MaxPriceStep * makerOption.BasePrice / 100);
                                //     }
                                //     else
                                //     {
                                //         price = 0;
                                //     }
                                //
                                //     price = Decimal.Truncate(makerOption.PriceFix);
                                //
                                //     if (price > 0)
                                //         await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                // }
                                //
                                // if (makerOption.MaxPriceOverStep > 0)
                                // {
                                //     if (makerOption.LastPrice)
                                //     {
                                //         price = RandomNumber(
                                //             bot.LastPrice + makerOption.MaxPriceOverStep *
                                //             bot.LastPrice / 100,
                                //             bot.LastPrice + (makerOption.MaxPriceStep + makerOption.MaxPriceOverStep) *
                                //             bot.LastPrice / 100);
                                //     }
                                //     else if (makerOption.BasePrice > 0)
                                //     {
                                //         price = RandomNumber(
                                //             makerOption.BasePrice + makerOption.MaxPriceOverStep *
                                //             makerOption.BasePrice / 100,
                                //             makerOption.BasePrice +
                                //             (makerOption.MaxPriceStep + makerOption.MaxPriceOverStep) *
                                //             makerOption.BasePrice / 100);
                                //     }
                                //     else
                                //     {
                                //         price = 0;
                                //     }
                                //
                                //     price = Decimal.Truncate(makerOption.PriceFix);
                                //
                                //     if (price > 0)
                                //         await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);
                                // }
                                //
                                // #endregion

                                #region BTC Spread

                                if (makerOption.IsFollowBtc && spreadHigh)
                                {
                                    //Buy more 
                                    if (price >= maxPrice)
                                    {
                                        var buyPrice = minPrice * (1 + spreadFixPercent / 100);
                                        buyPrice = buyPrice.Truncate(quotePrecision);
                                        qty /= 2;
                                        await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                            buyPrice.ToString($"F{quotePrecision}"), OrderSide.BUY);
                                    }
                                    //Sell more 
                                    else if (price <= minPrice)
                                    {
                                        var sellPrice = maxPrice * (1 - spreadFixPercent / 100);
                                        sellPrice = sellPrice.Truncate(quotePrecision);
                                        qty /= 2;
                                        await CreateLimitOrder(mexcClient, bot, qty.ToString($"F{basePrecision}"),
                                            sellPrice.ToString($"F{quotePrecision}"), OrderSide.SELL);
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

        private async Task<bool> CreateLimitOrder(MexcClient client, BotDto bot, string qty, string price,
            OrderSide side)
        {
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
            order.UserId = bot.UserId;
            order.ExpiredTime = order.TransactTime + MexcBotConstants.ExpiredOrderTime;

            var exec = await sqlConnection.ExecuteAsync(
                @"INSERT INTO BotOrders(BotId,UserId,OrderId,Symbol,OrderListId,Price,OrigQty,Type,Side,ExpiredTime,Status,`TransactTime`)
                      VALUES(@BotId,@UserId,@OrderId,@Symbol,@OrderListId,@Price,@OrigQty,@Type,@Side,@ExpiredTime,@Status,@TransactTime)",
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
                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET Logs = @Logs, Status = @Status, NextTime = @NextTime
                    WHERE Id = @Id",
                        bot);
                }
                else
                {
                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET NextTime = @NextTime
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