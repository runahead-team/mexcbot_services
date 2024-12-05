using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace mexcbot.Api.Jobs.DeepCoin
{
    public class DeepCoinVolMakerJob : BackgroundService
    {
        public DeepCoinVolMakerJob()
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

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND ExchangeType IN @ExchangeTypes AND (NextRunVolTime < @Now OR NextRunVolTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.VOLUME,
                            ExchangeTypes = new[] { BotExchangeType.DEEPCOIN },
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
                        Log.Error(e, "LbankVolMakerJob:CreateOrderJob");
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
                Log.Information("DeepCoin BOT {0} run", bot.Symbol);

                ExchangeClient client = bot.ExchangeType switch
                {
                    BotExchangeType.DEEPCOIN => new DeepCoinClient(Configurations.DeepCoinUrl, bot.ApiKey,
                        bot.ApiSecret, bot.Passphrase),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
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

                if (!balances.Any())
                {
                    bot.Status = BotStatus.INACTIVE;
                    stopLog += "Stop when your balances Zero\n";
                }
                else
                {
                    var baseBalance = balances.FirstOrDefault(x => x.Asset == bot.Base);

                    if (baseBalance == null || decimal.Parse(baseBalance.Free, new NumberFormatInfo()) <= 0)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"Stop when your {bot.Base} balance below 0 or null\n";
                    }

                    var quoteBalance = balances.FirstOrDefault(x => x.Asset == bot.Quote);

                    if (quoteBalance == null || decimal.Parse(quoteBalance.Free, new NumberFormatInfo()) <= 0)
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
                else
                {
                    //MinOrder
                    var minOrder = decimal.Parse(exchangeInfo.MinQty, new NumberFormatInfo());
                    if (bot.VolumeOptionObj.MinOrderQty > minOrder)
                    {
                        stopLog +=
                            $"Min quantity must be bigger than required quantity {minOrder}; \n";
                    }

                    var maxLimitQty = decimal.Parse(exchangeInfo.MaxLimitQty, new NumberFormatInfo());

                    if (bot.VolumeOptionObj.MaxOrderQty > maxLimitQty)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog +=
                            $"Min quantity must be less than required quantity {maxLimitQty}; \n";
                    }
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
                    await UpdateBot(bot);
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
                    var btcUsdVol24hr = decimal.Parse((await client.GetTicker24hr("BTC", "USDT"))?.QuoteVolume,
                        new NumberFormatInfo());
                    var botUsdVol24hr = decimal.Parse(botTicker24hr.QuoteVolume, new NumberFormatInfo());
                    var botLastPrice = decimal.Parse(botTicker24hr.LastPrice, new NumberFormatInfo());
                    var rateVol24hr = volumeOption.Volume24hr / btcUsdVol24hr;

                    Log.Warning($"btcUsdVol24hr {btcUsdVol24hr.ToString()} & rateVol24hr {rateVol24hr.ToString()}");

                    if (botLastPrice <= 0)
                    {
                        Log.Warning("botLastPrice zero");
                        return;
                    }

                    if (botUsdVol24hr >= volumeOption.Volume24hr)
                    {
                        Log.Warning($"Volume enough {botUsdVol24hr.ToString()}");
                        return;
                    }

                    //NOTE: 0-Open time, 1-Open, 2-High, 3-Low, 4-Close, 5-Volume, 6-Close time, 7-Quote asset volume
                    //DeepCoin: Enum:"1m","5m","15m","30m","1H","4H","12H","1D","1W","1M","1Y"
                    var type5m = "5m";

                    var btcCandleStick5m = await client.GetCandleStick("BTC", "USDT", type5m);

                    var botCandleStick5m = (await client.GetCandleStick(bot.Base, bot.Quote, type5m));

                    if (btcCandleStick5m.Count <= 0 && botCandleStick5m.Count <= 0)
                        Log.Warning("Get Candle Stick fail");

                    var btcCandleStickPre1Day =
                        btcCandleStick5m.Where(x => x[0].Value<long>() >= preStartDate).ToList();

                    var botCandleStickOnDay = botCandleStick5m.Where(x => x[0].Value<long>() >= startDate).ToList();

                    var botCandleStickAtNow = botCandleStickOnDay.LastOrDefault(x => x[0].Value<long>() <= nowDate);

                    var btcCandleStickPredict =
                        btcCandleStickPre1Day.LastOrDefault(x => x[0].Value<long>() <= preDate);

                    if ((btcCandleStickPredict == null || botCandleStickAtNow == null))
                    {
                        Log.Warning("Candlestick null");
                        return;
                    }

                    var btcUsdVolumePredict =
                        decimal.Parse(btcCandleStickPredict[5].Value<string>(), new NumberFormatInfo())
                        * decimal.Parse(
                            btcCandleStickPredict[4].Value<string>(), new NumberFormatInfo());

                    Log.Warning($"btcUsdVolume5m 1day before {btcUsdVolumePredict.ToString()}");

                    var botUsdVolumeTarget = rateVol24hr * btcUsdVolumePredict;

                    var botUsdVolumeReal = decimal.Parse(botCandleStickAtNow[5].Value<string>(), new NumberFormatInfo())
                                           * decimal.Parse(botCandleStickAtNow[4].Value<string>(),
                                               new NumberFormatInfo());

                    var botUsdOrderValue = botUsdVolumeTarget - botUsdVolumeReal;

                    Log.Warning($"botUsdOrderValue5m {botUsdOrderValue.ToString()}");

                    Log.Warning($"botUsdVolumeTarget5m {botUsdVolumeTarget.ToString()}");

                    Log.Warning($"botUsdVolumeReal5m {botUsdVolumeReal.ToString()}");

                    //If volume 5m >= predict
                    if (botUsdOrderValue > botUsdVolumeTarget)
                    {
                        Log.Error("Volume enough");
                        return;
                    }

                    #endregion

                    var avgOrder = (volumeOption.MinOrderQty + volumeOption.MaxOrderQty) / 2;
                    var numOfOrder = (int)(botUsdOrderValue / botLastPrice / avgOrder);

                    if (numOfOrder <= 0)
                    {
                        Log.Error("No order");

                        bot.NextRunVolTime = now + MexcBotConstants.BotVolInterval;

                        await UpdateBot(bot, false);
                        return;
                    }

                    //5m/numOfOrder => delay time between 2 orders;
                    //if numOfOrder > 5m => 30m/numOfOrder => delay time between 2 orders;
                    var millisecond5m = TimeSpan.FromMinutes(5).TotalMilliseconds;
                    var maxTimeToMatch = volumeOption.MatchingDelayTo != 0
                        ? numOfOrder * (volumeOption.MatchingDelayTo * 1000)
                        : numOfOrder;

                    var timing = maxTimeToMatch > millisecond5m ? millisecond5m * 6 : millisecond5m;
                    var delayOrder = (int)timing / maxTimeToMatch;

                    //if delayOrder==0 => total time to complete is more than 30m
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

                            var orderQty = Math.Round(
                                RandomNumber(volumeOption.MinOrderQty, volumeOption.MaxOrderQty, basePrecision),
                                basePrecision);

                            totalQty += orderQty;

                            botTicker24hr = (await client.GetTicker24hr(bot.Base, bot.Quote));
                            var orderPrice = decimal.Parse(botTicker24hr.LastPrice, new NumberFormatInfo());

                            if (orderPrice < 0)
                            {
                                Log.Warning("askPrice zero");
                                return;
                            }

                            var orderbook = await client.GetOrderbook(bot.Base, bot.Quote);
                            var asks = orderbook.Asks;
                            var bids = orderbook.Bids;

                            if (orderbook.Asks.Count == 0 || orderbook.Bids.Count == 0)
                                return;

                            var smallestAskPrice = asks[0][0];
                            var biggestBidPrice = bids[0][0];

                            // if (volumeOption.SafeRun)
                            // {
                            //     if (orderPrice >= smallestAskPrice)
                            //         return;
                            //     if (orderPrice <= biggestBidPrice)
                            //         return;
                            // }

                            totalUsdVolume += orderQty * orderPrice;

                            if (orderQty < 0)
                            {
                                Log.Warning("orderQty zero");
                                return;
                            }

                            if (volumeOption.MatchingDelayFrom == 0 || volumeOption.MatchingDelayTo == 0)
                            {
                                var tasks = new List<Task>();

                                if (volumeOption.Side is OrderSide.SELL or OrderSide.BOTH)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL));
                                }

                                if (volumeOption.Side is OrderSide.BUY or OrderSide.BOTH)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
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
                                        orderPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.SELL);
                                }

                                if (volumeOption.Side is OrderSide.BUY or OrderSide.BOTH)
                                {
                                    await TradeDelay(bot);

                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.BUY);
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

            var msg = side + " " + qty + " " + bot.Symbol + " at price " + " " + price + " " + order.OrderId;
            Log.Information("Bot create order {0}", msg);

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

            order.Symbol = bot.Symbol;
            order.Price = price;
            order.OrigQty = qty;
            order.Type = bot.ExchangeType == BotExchangeType.DEEPCOIN ? "limit" : order.Type;
            order.BotId = bot.Id;
            order.BotType = bot.Type;
            order.BotExchangeType = bot.ExchangeType;
            order.UserId = bot.UserId;
            order.TransactTime = AppUtils.NowMilis();
            
            order.ExpiredTime = bot.ExchangeType == BotExchangeType.DEEPCOIN ? AppUtils.NowMilis(): order.TransactTime;

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

                await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET Logs = @Logs, NextRunVolTime = @NextRunVolTime
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