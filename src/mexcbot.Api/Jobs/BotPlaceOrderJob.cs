using System;
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
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class BotPlaceOrderJob : BackgroundService
    {
        public BotPlaceOrderJob()
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
                        "SELECT * FROM Bots WHERE Status = @Status", new
                        {
                            Status = BotStatus.ACTIVE
                        })).ToList();

                    if (!bots.Any())
                        return;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);

                    ver++;

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotPlaceOrderJob:CreateOrderJob");
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

                #region Validate

                var balances = await mexcClient.GetAccInformation();

                if (!balances.Any())
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += "Stop when your balances Zero\n";
                }
                else
                {
                    var baseBalance = balances.FirstOrDefault(x => x.Asset == bot.Base);

                    if (baseBalance == null || decimal.Parse(baseBalance.Free) <= 0)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"Stop when your {bot.Base} balance below 0 or null\n";
                    }

                    var quoteBalance = balances.FirstOrDefault(x => x.Asset == bot.Quote);

                    if (quoteBalance == null || decimal.Parse(quoteBalance.Free) <= 0)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"Stop when your {bot.Quote} balance below 0 or null\n";
                    }
                }

                if (exchangeInfo == null)
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when exchange info not found\n";
                }

                //Stop
                if (bot.Status == BotStatus.INACTIVE)
                {
                    bot.Logs = stopLog;
                    await UpdateBot(bot);
                    return;
                }

                #endregion

                #region Follow btc candle 60m => bot's volume 60m

                var startDate = now - (now % 86400000);
                var endDate = startDate + 86400000;
                var nowDate = now;

                var preStartDate = startDate - 86400000;
                var preEndDate = startDate;
                var preDate = nowDate - 86400000;

                var botTicker24hr = (await mexcClient.GetTicker24hr(bot.Base, bot.Quote));
                var btcUsdVol24hr = decimal.Parse((await mexcClient.GetTicker24hr("BTC", "USDT")).QuoteVolume);
                var botUsdVol24hr = decimal.Parse(botTicker24hr.QuoteVolume);
                var botLastPrice = decimal.Parse(botTicker24hr.LastPrice);
                var rateVol24hr = bot.Volume24hr / btcUsdVol24hr;

                if (botUsdVol24hr >= bot.Volume24hr)
                    return;

                //NOTE: 0-Open time, 1-Open, 2-High, 3-Low, 4-Close, 5-Volume, 6-Close time, 7-Quote asset volume
                //NOTE: 1m, 5m, 15m, 30m, 60m, 4h, 1d, 1M
                var btcCandleStick5m = await mexcClient.GetCandleStick("BTC", "USDT", "5m");

                var botCandleStick5m = await mexcClient.GetCandleStick(bot.Base, bot.Quote, "5m");

                var btcCandleStickPre1Day =
                    btcCandleStick5m.Where(x => x[0].Value<long>() >= preStartDate && x[6].Value<long>() <= preEndDate)
                        .ToList();

                var botCandleStickOnDay =
                    botCandleStick5m.Where(x => x[0].Value<long>() >= startDate && x[6].Value<long>() <= endDate)
                        .ToList();

                var botCandleStickAtNow =
                    botCandleStickOnDay.FirstOrDefault(x =>
                        x[0].Value<long>() <= nowDate && nowDate <= x[6].Value<long>());

                var btcCandleStickPredict =
                    btcCandleStickPre1Day.FirstOrDefault(x =>
                        x[0].Value<long>() <= preDate && preDate <= x[6].Value<long>());

                if (btcCandleStickPredict == null || botCandleStickAtNow == null)
                    return;

                var btcUsdVolumePredict = decimal.Parse(btcCandleStickPredict[7].Value<string>());

                var botUsdVolumeTarget = rateVol24hr * btcUsdVolumePredict;

                var botUsdVolumeReal = decimal.Parse(botCandleStickAtNow[7].Value<string>());

                var botUsdOrderValue = botUsdVolumeTarget - botUsdVolumeReal;

                //If volume 5m >= predict
                if (botUsdOrderValue > botUsdVolumeTarget)
                    return;

                #endregion

                var avgOrder = (bot.MinOrderQty + bot.MaxOrderQty) / 2;
                var numOfOrder = (int)(botUsdOrderValue / botLastPrice / avgOrder);

                if (numOfOrder == 0)
                    return;

                
                //5m/numOfOrder => delay time between 2 orders;
                var delayOrder = (int)TimeSpan.FromMinutes(5).TotalMilliseconds / numOfOrder;

                if (bot.MatchingDelayTo != 0)
                {
                    delayOrder -= (int)(bot.MatchingDelayTo * 1000);
                }

                var totalQty = 0m;
                var totalUsdVolume = 0m;
                var fromTime = AppUtils.NowMilis();

                var quotePrecision = exchangeInfo.QuoteAssetPrecision;
                var basePrecision = exchangeInfo.BaseAssetPrecision;
                for (var i = 0; i < numOfOrder; i++)
                {
                    try
                    {
                        var orderbook = await mexcClient.GetOrderbook(bot.Base, bot.Quote);

                        if (orderbook.Asks.Count == 0 || orderbook.Asks.Count == 0)
                            continue;

                        var orderQty = Math.Round(RandomNumber(bot.MinOrderQty, bot.MaxOrderQty, basePrecision),
                            basePrecision);

                        totalQty += orderQty;

                        //Ask [Price, Quantity ]
                        var asks = orderbook.Asks;
                        var bids = orderbook.Bids;

                        var smallestAskPrice = asks[0][0];
                        var biggestBidPrice = bids[0][0];
                        var askPrice = 0m;
                        var sizePrediction = 1 / (decimal)Math.Pow(10, quotePrecision);

                        askPrice = biggestBidPrice + sizePrediction == smallestAskPrice
                            ? smallestAskPrice
                            : RandomNumber(biggestBidPrice, smallestAskPrice, quotePrecision);

                        totalUsdVolume += orderQty * askPrice;

                        #region Validation balance

                        var checkBalances = await mexcClient.GetAccInformation();

                        var baseBalance = decimal.Parse(checkBalances.FirstOrDefault(x => x.Asset == bot.Base).Free);

                        if (baseBalance <= orderQty)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when your {bot.Base} balance below sell's order quantity\n";
                        }

                        var quoteBalance = decimal.Parse(balances.FirstOrDefault(x => x.Asset == bot.Quote).Free);

                        if (quoteBalance <= orderQty * askPrice)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += $"Stop when your {bot.Quote} balance below buy's order quantity\n";
                        }

                        //Stop
                        if (bot.Status == BotStatus.INACTIVE)
                        {
                            bot.Logs = stopLog;
                            await UpdateBot(bot);
                            return;
                        }

                        #endregion

                        if (bot.MatchingDelayFrom == 0 || bot.MatchingDelayTo == 0)
                        {
                            var sellTask = CreateLimitOrder(mexcClient, bot, orderQty.ToString($"F{basePrecision}"),
                                askPrice.ToString($"F{quotePrecision}"), OrderSide.SELL);
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                            var buyTask = CreateLimitOrder(mexcClient, bot, orderQty.ToString($"F{basePrecision}"),
                                askPrice.ToString($"F{quotePrecision}"), OrderSide.BUY);
                            await Task.WhenAll(sellTask, buyTask);
                        }
                        else
                        {
                            await CreateLimitOrder(mexcClient, bot, orderQty.ToString($"F{basePrecision}"),
                                askPrice.ToString($"F{quotePrecision}"), OrderSide.SELL);
                            await TradeDelay(bot);
                            await CreateLimitOrder(mexcClient, bot, orderQty.ToString($"F{basePrecision}"),
                                askPrice.ToString($"F{quotePrecision}"), OrderSide.BUY);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e,"Bot Order");
                    }
                    finally
                    {
                        Log.Information("Delay {0}s", TimeSpan.FromMilliseconds(delayOrder).Seconds);
                        
                        await Task.Delay(TimeSpan.FromMilliseconds(delayOrder));
                    }
                }

                var toTime = AppUtils.NowMilis();

                Log.Information(
                    $"Summary: totalQty={totalQty} & totalUsdVolume={totalUsdVolume} & fromTime={fromTime} & toTime={toTime}");
            }
            catch (Exception e)
            {
                Log.Error(e,"Bot run");
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

            if(string.IsNullOrEmpty(order.OrderId))
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

        private async Task UpdateBot(BotDto bot)
        {
            try
            {
                await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                await dbConnection.OpenAsync();

                await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET Logs = @Logs, Status = @Status
                    WHERE Id = @Id",
                    bot);
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateBot");
            }
        }

        private async Task TradeDelay(BotDto bot)
        {
            await Task.Delay((int)RandomNumber(bot.MatchingDelayFrom,
                bot.MatchingDelayTo, 1) * 1000);
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