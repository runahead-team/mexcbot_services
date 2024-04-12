using System;
using System.Collections.Generic;
using System.Globalization;
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
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class BotVolPlaceOrderJob : BackgroundService
    {
        public BotVolPlaceOrderJob()
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
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND ExchangeType = @ExchangeType AND (NextRunVolTime < @Now OR NextRunVolTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.VOLUME,
                            ExchangeType = BotExchangeType.MEXC,
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    if (!bots.Any())
                        continue;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);

                    ver++;
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotVolPlaceOrderJob:CreateOrderJob");
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
                    _ => throw new ArgumentOutOfRangeException()
                };

                var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
                var selfSymbols = await client.GetSelfSymbols();
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

                #region Validate

                if (!selfSymbols.Contains(bot.Symbol))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"{bot.Symbol} is not support\n";
                }

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

                //default
                bot.NextRunVolTime = now;

                if (string.IsNullOrEmpty(bot.VolumeOption))
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += $"Stop when volume option is null\n";
                }
                else
                {
                    bot.NextRunVolTime =
                        now + (int)RandomNumber(bot.VolumeOptionObj.MinInterval, bot.VolumeOptionObj.MaxInterval, 0) *
                        1000;
                }

                //Stop
                if (bot.Status == BotStatus.INACTIVE)
                {
                    bot.Logs = stopLog;
                    await UpdateBot(bot,false);
                    return;
                }
                else
                {
                    await UpdateBot(bot, false);
                }

                #endregion

                #region Follow btc candle 60m => bot's volume 60m

                var startDate = now - (now % 86400000);
                var endDate = startDate + 86400000;
                var nowDate = now;

                var preStartDate = startDate - 86400000;
                var preEndDate = startDate;
                var preDate = nowDate - 86400000;

                if (bot.VolumeOption != null)
                {
                    var volumeOption = JsonConvert.DeserializeObject<BotVolumeOption>(bot.VolumeOption);
                    var botTicker24hr = (await client.GetTicker24hr(bot.Base, bot.Quote));
                    var btcUsdVol24hr = decimal.Parse((await client.GetTicker24hr("BTC", "USDT"))?.QuoteVolume);
                    var botUsdVol24hr = decimal.Parse(botTicker24hr.QuoteVolume);
                    var botLastPrice = decimal.Parse(botTicker24hr.LastPrice);
                    var rateVol24hr = volumeOption.Volume24hr / btcUsdVol24hr;

                    Log.Warning($"btcUsdVol24hr {btcUsdVol24hr.ToString()} & rateVol24hr {rateVol24hr.ToString()}");

                    if (botLastPrice <= 0)
                    {
                        Log.Warning("botLastPrice zero");
                        return;
                    }

                    if (botUsdVol24hr >= volumeOption.Volume24hr)
                    {
                        Log.Warning("Volume enough");
                        return;
                    }

                    //NOTE: 0-Open time, 1-Open, 2-High, 3-Low, 4-Close, 5-Volume, 6-Close time, 7-Quote asset volume
                    //NOTE: 1m, 5m, 15m, 30m, 60m, 4h, 1d, 1M

                    var btcCandleStick5m = await client.GetCandleStick("BTC", "USDT", "5m");

                    var botCandleStick5m = await client.GetCandleStick(bot.Base, bot.Quote, "5m");

                    if (btcCandleStick5m.Count <= 0 && botCandleStick5m.Count <= 0)
                        Log.Warning("Get candle stick fail");

                    var btcCandleStickPre1Day =
                        btcCandleStick5m.Where(x =>
                                x[0].Value<long>() >= preStartDate && x[6].Value<long>() <= preEndDate)
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
                    {
                        Log.Warning("Candlestick null");
                        return;
                    }

                    var btcUsdVolumePredict = decimal.Parse(btcCandleStickPredict[7].Value<string>());

                    Log.Warning($"btcUsdVolume5m 1day before {btcUsdVolumePredict}");

                    var botUsdVolumeTarget = rateVol24hr * btcUsdVolumePredict;

                    var botUsdVolumeReal = decimal.Parse(botCandleStickAtNow[7].Value<string>());

                    var botUsdOrderValue = botUsdVolumeTarget - botUsdVolumeReal;

                    Log.Warning($"botUsdOrderValue5m {botUsdOrderValue}");

                    Log.Warning($"botUsdVolumeTarget5m {botUsdVolumeTarget}");

                    Log.Warning($"botUsdVolumeReal5m {botUsdVolumeReal}");

                    //If volume 5m >= predict
                    if (botUsdOrderValue > botUsdVolumeTarget)
                    {
                        Log.Warning("Volume enough");
                        return;
                    }

                    #endregion

                    var avgOrder = (volumeOption.MinOrderQty + volumeOption.MaxOrderQty) / 2;
                    var numOfOrder = (int)(botUsdOrderValue / botLastPrice / avgOrder);

                    if (numOfOrder <= 0)
                    {
                        Log.Warning("No order");

                        bot.NextRunVolTime = now + MexcBotConstants.BotVolInterval;

                        await UpdateBot(bot, false);
                        return;
                    }

                    //5m/numOfOrder => delay time between 2 orders;
                    var delayOrder = (int)TimeSpan.FromMinutes(5).TotalMilliseconds / numOfOrder;

                    if (volumeOption.MatchingDelayTo != 0)
                    {
                        delayOrder -= (int)(volumeOption.MatchingDelayTo * 1000);
                    }

                    if (delayOrder < 0)
                    {
                        Log.Error("Delay order can not below 0");
                        return;
                    }

                    var totalQty = 0m;
                    var totalUsdVolume = 0m;
                    var fromTime = AppUtils.NowMilis();

                    var quotePrecision = exchangeInfo.QuoteAssetPrecision;
                    var basePrecision = exchangeInfo.BaseAssetPrecision;

                    Log.Information("numOfOrder {0}", numOfOrder);

                    for (var i = 0; i < numOfOrder; i++)
                    {
                        try
                        {
                            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                            if (await dbConnection.ExecuteScalarAsync<int>(
                                    "SELECT COUNT(0) FROM Bots WHERE Id = @Id AND Status = @Status", new
                                    {
                                        Id = bot.Id,
                                        Status = BotStatus.ACTIVE
                                    }) != 1)
                            {
                                Log.Warning("Bot stop");
                                return;
                            }

                            Log.Information("order #{0}", i + 1);

                            var orderbook = await client.GetOrderbook(bot.Base, bot.Quote);

                            if (orderbook.Asks.Count == 0 || orderbook.Asks.Count == 0)
                            {
                                Log.Warning("Orderbook empty");
                                return;
                            }

                            var orderQty = Math.Round(
                                RandomNumber(volumeOption.MinOrderQty, volumeOption.MaxOrderQty, basePrecision),
                                basePrecision);

                            totalQty += orderQty;

                            //Ask [Price, Quantity ]
                            var asks = orderbook.Asks;
                            var bids = orderbook.Bids;

                            var smallestAskPrice = asks[0][0];
                            var biggestBidPrice = bids[0][0];
                            var priceStep = 1 / (decimal)Math.Pow(10, quotePrecision);
                            var askPrice = 0m;
                            var noBuy = false;

                            if (smallestAskPrice - biggestBidPrice > priceStep * 50)
                            {
                                askPrice = RandomNumber(biggestBidPrice + priceStep * 20,
                                    smallestAskPrice - priceStep * 20,
                                    quotePrecision);
                            }
                            else if (smallestAskPrice - biggestBidPrice > priceStep * 20)
                            {
                                askPrice = RandomNumber(biggestBidPrice + priceStep * 10,
                                    smallestAskPrice - priceStep * 10,
                                    quotePrecision);
                            }
                            else if (smallestAskPrice - biggestBidPrice > priceStep * 10)
                            {
                                askPrice = RandomNumber(biggestBidPrice + priceStep * 5,
                                    smallestAskPrice - priceStep * 5,
                                    quotePrecision);
                            }
                            else if (smallestAskPrice - biggestBidPrice > priceStep * 5)
                            {
                                askPrice = RandomNumber(biggestBidPrice + priceStep * 1,
                                    smallestAskPrice - priceStep * 1,
                                    quotePrecision);
                            }
                            else
                            {
                                if (smallestAskPrice - biggestBidPrice <= priceStep * 1)
                                {
                                    if (volumeOption.SafeRun)
                                        return;

                                    askPrice = biggestBidPrice;
                                    noBuy = true;
                                }
                                else
                                {
                                    if (volumeOption.SafeRun)
                                        noBuy = true;

                                    askPrice = RandomNumber(biggestBidPrice + priceStep,
                                        smallestAskPrice - priceStep,
                                        quotePrecision);
                                }
                            }

                            totalUsdVolume += orderQty * askPrice;

                            if (orderQty < 0)
                            {
                                Log.Warning("orderQty zero");
                                return;
                            }

                            if (askPrice < 0)
                            {
                                Log.Warning("Price zero");
                                return;
                            }

                            if (volumeOption.MatchingDelayFrom == 0 || volumeOption.MatchingDelayTo == 0)
                            {
                                var tasks = new List<Task>();

                                if (volumeOption.Side is OrderSide.SELL or OrderSide.BOTH)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        askPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL));
                                }

                                if (!noBuy && volumeOption.Side is OrderSide.BUY or OrderSide.BOTH)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        askPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.BUY));
                                }

                                await Task.WhenAll(tasks);
                            }
                            else
                            {
                                if (volumeOption.Side is OrderSide.SELL or OrderSide.BOTH)
                                {
                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        askPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.SELL);
                                }

                                if (!noBuy && volumeOption.Side is OrderSide.BUY or OrderSide.BOTH)
                                {
                                    await TradeDelay(bot);

                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        askPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()), OrderSide.BUY);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Bot order");
                        }

                        Log.Information("Delay {0}s", TimeSpan.FromMilliseconds(delayOrder).TotalSeconds);

                        await Task.Delay(TimeSpan.FromMilliseconds(delayOrder));
                    }

                    var toTime = AppUtils.NowMilis();

                    Log.Information(
                        $"Summary: totalQty={totalQty} & totalUsdVolume={totalUsdVolume} & fromTime={fromTime} & toTime={toTime}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Bot run");
            }
        }

        #region Private

        private async Task<bool> CreateLimitOrder(ExchangeClient client, BotDto bot, string qty, string price,
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
            order.BotType = bot.Type;
            order.BotExchangeType = bot.ExchangeType;
            order.UserId = bot.UserId;
            order.ExpiredTime = order.TransactTime;

            var exec = await sqlConnection.ExecuteAsync(
                @"INSERT INTO BotOrders(BotId,BotType,BotExchangeType,UserId,OrderId,Symbol,OrderListId,Price,OrigQty,Type,Side,ExpiredTime,Status,`TransactTime`)
                      VALUES(@BotId,@BotType,@BotExchangeType,@UserId,@OrderId,@Symbol,@OrderListId,@Price,@OrigQty,@Type,@Side,@ExpiredTime,@Status,@TransactTime)",
                order);

            if (exec == 0)
                Log.Error("Bot insert order fail {@data}", order);

            return true;
        }

        private async Task UpdateBot(BotDto bot, bool isInActive = true)
        {
            try
            {
                await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                await dbConnection.OpenAsync();

                if (isInActive)
                {
                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET Logs = @Logs, Status = @Status
                    WHERE Id = @Id",
                        bot);
                }
                else
                {
                    await dbConnection.ExecuteAsync(
                        @"UPDATE Bots SET NextRunVolTime = @NextRunVolTime
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
            var volumeOption = JsonConvert.DeserializeObject<BotVolumeOption>(bot.VolumeOption);

            await Task.Delay((int)RandomNumber(volumeOption.MatchingDelayFrom,
                volumeOption.MatchingDelayTo, 1) * 1000);
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