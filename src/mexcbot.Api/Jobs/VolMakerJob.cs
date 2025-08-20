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
using mexcbot.Api.Infrastructure.Telegram;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.Services.Interface;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class VolMarkerJob : BackgroundService
    {
        private readonly IBotService _botService;

        public VolMarkerJob(IBotService botService)
        {
            _botService = botService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>
            {
                CreateOrderJob(stoppingToken, BotExchangeType.MEXC),
                CreateOrderJob(stoppingToken, BotExchangeType.COINSTORE),
                CreateOrderJob(stoppingToken, BotExchangeType.GATE)
            };

            await Task.WhenAll(tasks);
        }

        private async Task CreateOrderJob(CancellationToken stoppingToken, BotExchangeType exchangeType)
        {
            var ver = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND `Type` = @Type AND `ExchangeType` IN @ExchangeTypes AND (NextRunVolTime < @Now OR NextRunVolTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.VOLUME,
                            ExchangeTypes = new[] { exchangeType },
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    if (!bots.Any())
                        continue;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);

                    ver++;
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    Log.Error(e, "MolMarkerJob:CreateOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }

        private async Task Run(BotDto bot)
        {
            var stopLog = "";
            var now = AppUtils.NowMilis();

            try
            {
                Log.Information("VOLBOT {0} #{1} {2} run", bot.Symbol, bot.Id, bot.ExchangeType.ToString("G"));

                MemCache.AddActiveBot(bot);

                ExchangeClient client = bot.ExchangeType switch
                {
                    BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.COINSTORE => new CoinStoreClient(Configurations.CoinStoreUrl, bot.ApiKey,
                        bot.ApiSecret),
                    BotExchangeType.GATE => new GateClient(Configurations.GateUrl, bot.ApiKey,
                        bot.ApiSecret),
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
                    var baseBalance = balances.FirstOrDefault(x =>
                        string.Equals(x.Asset, bot.Base, StringComparison.InvariantCultureIgnoreCase));

                    if (baseBalance == null || decimal.Parse(baseBalance.Free, new NumberFormatInfo()) <= 0)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"Stop when your {bot.Base} balance below 0 or null\n";
                        Telegram.Send($"🟠 VOL BOT {bot.Base} is stopped by balance {bot.Base} = 0 or null");
                    }
                    else
                    {
                        if (decimal.TryParse(baseBalance.Free, new NumberFormatInfo(), out var baseBalanceValue))
                            if (bot.VolumeOptionObj.StopLossBase > 0)
                                if (baseBalanceValue <= bot.VolumeOptionObj.StopLossBase)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog +=
                                        $"Stop when your {bot.Base} balance lower than {bot.VolumeOptionObj.StopLossBase}; \n";
                                    Telegram.Send(
                                        $"🟠 VOL BOT {bot.Base} is stopped by balance {bot.Base}: {baseBalanceValue:N} < {bot.VolumeOptionObj.StopLossBase:N}");
                                }
                    }


                    var quoteBalance = balances.FirstOrDefault(x =>
                        string.Equals(x.Asset, bot.Quote, StringComparison.InvariantCultureIgnoreCase));

                    if (quoteBalance == null || decimal.Parse(quoteBalance.Free, new NumberFormatInfo()) <= 0)
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"Stop when your {bot.Quote} balance below 0 or null\n";
                        Telegram.Send($"🟠 VOL BOT {bot.Base} is stopped by balance {bot.Quote} = 0 or null");
                    }
                    else
                    {
                        if (decimal.TryParse(quoteBalance.Free, new NumberFormatInfo(), out var quoteBalanceValue))
                            if (bot.VolumeOptionObj.StopLossBase > 0)
                                if (quoteBalanceValue <= bot.VolumeOptionObj.StopLossQuote)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog +=
                                        $"Stop when your {bot.Base} balance lower than {bot.VolumeOptionObj.StopLossQuote}; \n";
                                    Telegram.Send(
                                        $"🟠 VOL BOT {bot.Base} is stopped by balance {bot.Quote}: {quoteBalanceValue:N} < {bot.VolumeOptionObj.StopLossQuote:N}");
                                }
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
                        now + (int)RandomNumber(bot.VolumeOptionObj.MinInterval, bot.VolumeOptionObj.MaxInterval,
                            0) *
                        1000;
                }

                //Stop
                if (bot.Status == BotStatus.INACTIVE)
                {
                    bot.Logs = stopLog;
                    await UpdateBot(bot, false);
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

                    var quotePrecision = bot.QuotePrecision ?? 8;
                    var basePrecision = bot.BasePrecision ?? 0;

                    var orderbook0 = await client.GetOrderbook(bot.Base, bot.Quote);
                    if (orderbook0.Asks.Count == 0 || orderbook0.Bids.Count == 0)
                        return;

                    var smallestAskPrice0 = orderbook0.Asks[0][0];
                    var biggestBidPrice0 = orderbook0.Bids[0][0];
                    var spread = smallestAskPrice0 - biggestBidPrice0;

                    decimal usdLiqRequired = 1000;

                    if (bot.Base == "FISHW")
                    {
                        usdLiqRequired = 1000;
                    }

                    if (bot.Base == "FISHW" && bot.ExchangeType == BotExchangeType.MEXC)
                    {
                        var midPrice = Math.Round((smallestAskPrice0 + biggestBidPrice0) / 2,
                            bot.QuotePrecision ?? 8);

                        var sleepTime = (int)(usdLiqRequired /
                                              (midPrice * (volumeOption.MinOrderQty + volumeOption.MaxOrderQty) / 2)) *
                                        volumeOption.MinInterval;

                        var maxAsk = midPrice * 1.02m;
                        var totalAsk = orderbook0.Asks
                            .Where(x => x[0] <= maxAsk)
                            .Sum(x => x[0] * x[1]);


                        if (totalAsk < usdLiqRequired)
                        {
                            for (var i = 0; i < 10; i++)
                            {
                                var orderPrice = Math.Round(RandomNumber(midPrice, maxAsk, quotePrecision),
                                    quotePrecision);

                                var orderQty =
                                    Math.Round(
                                        RandomNumber(volumeOption.MinOrderQty, volumeOption.MaxOrderQty, basePrecision),
                                        basePrecision);

                                await CreateLimitOrder(client, bot,
                                    orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                    orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                    OrderSide.SELL, sleepTime + i * volumeOption.MinInterval);

                                totalAsk += orderQty * orderPrice;

                                if (totalAsk > usdLiqRequired)
                                    break;

                                await Task.Delay(TimeSpan.FromSeconds(1));
                            }
                        }

                        var minBid = midPrice * 0.98m;
                        var totalBid = orderbook0.Bids
                            .Where(x => x[0] >= minBid)
                            .Sum(x => x[0] * x[1]);

                        if (totalBid < usdLiqRequired)
                        {
                            for (var i = 0; i < 10; i++)
                            {
                                var orderPrice = Math.Round(RandomNumber(minBid, midPrice, quotePrecision),
                                    quotePrecision);

                                var orderQty =
                                    Math.Round(
                                        RandomNumber(volumeOption.MinOrderQty, volumeOption.MaxOrderQty, basePrecision),
                                        basePrecision);

                                await CreateLimitOrder(client, bot,
                                    orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                    orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                    OrderSide.BUY, sleepTime + i * volumeOption.MinInterval);

                                totalBid += orderQty * orderPrice;

                                if (totalBid > usdLiqRequired)
                                    break;

                                await Task.Delay(TimeSpan.FromSeconds(1));
                            }
                        }
                    }

                    var botTicker24hr = (await client.GetTicker24hr(bot.Base, bot.Quote));
                    var btcUsdVol24hr = decimal.Parse((await client.GetTicker24hr("BTC", "USDT"))?.QuoteVolume,
                        new NumberFormatInfo());
                    var botUsdVol24hr = decimal.Parse(botTicker24hr.QuoteVolume, new NumberFormatInfo());
                    var botLastPrice = decimal.Parse(botTicker24hr.LastPrice, new NumberFormatInfo());
                    var rateVol24hr = volumeOption.Volume24hr / btcUsdVol24hr;

                    //todo random vol
                    rateVol24hr = rateVol24hr * (1 + (decimal)DateTime.UtcNow.Date.Day % 15 / 100);

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

                    //CoinStore: //1min, 5min, 15min, 30min, 60min, 4hour, 12hour, 1day, 1week 
                    var interval5M = "5m";
                    if (bot.ExchangeType == BotExchangeType.COINSTORE)
                        interval5M = "5min";

                    var btcCandleStick5m = await client.GetCandleStick("BTC", "USDT", interval5M);

                    var botCandleStick5m = await client.GetCandleStick(bot.Base, bot.Quote, interval5M);

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

                    var btcUsdVolumePredict =
                        decimal.Parse(btcCandleStickPredict[7].Value<string>(), new NumberFormatInfo());

                    // Log.Warning($"btcUsdVolume5m 1day before {btcUsdVolumePredict}");

                    var botUsdVolumeTarget = rateVol24hr * btcUsdVolumePredict;

                    var botUsdVolumeReal =
                        decimal.Parse(botCandleStickAtNow[7].Value<string>(), new NumberFormatInfo());

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

                    if (orderbook0.Asks.Count == 0 || orderbook0.Bids.Count == 0)
                        return;

                    await _botService.UpdateBotHistory(new BotHistoryDto
                    {
                        BotId = bot.Id,
                        Spread = spread,
                        BalanceBase = balances
                            .FirstOrDefault(x =>
                                string.Equals(x.Asset, bot.Base, StringComparison.InvariantCultureIgnoreCase))
                            ?.Free,
                        BalanceQuote = balances
                            .FirstOrDefault(x => string.Equals(x.Asset, bot.Quote,
                                StringComparison.InvariantCultureIgnoreCase))
                            ?.Free
                    });

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

                            decimal orderPrice = 0;

                            var orderbook = await client.GetOrderbook(bot.Base, bot.Quote);
                            var asks = orderbook.Asks;
                            var bids = orderbook.Bids;

                            if (orderbook.Asks.Count == 0 || orderbook.Bids.Count == 0)
                                return;

                            var smallestAskPrice = asks[0][0];
                            var biggestBidPrice = bids[0][0];
                            spread = smallestAskPrice - biggestBidPrice;

                            var unit = 1 / (decimal)Math.Pow(10, quotePrecision);

                            if (volumeOption.Side is OrderSide.SELL or OrderSide.BOTH)
                                orderPrice = smallestAskPrice - unit;
                            else if (volumeOption.Side is OrderSide.BUY)
                                orderPrice = biggestBidPrice + unit;

                            if (spread <= unit)
                                orderPrice = biggestBidPrice;

                            if (volumeOption.SafeRun)
                            {
                                orderPrice = (smallestAskPrice + biggestBidPrice) / 2;

                                if (orderPrice >= smallestAskPrice)
                                {
                                    Log.Information("VolBot {0} safe run (ask) {1} {2}", bot.Base, orderPrice,
                                        smallestAskPrice);
                                    return;
                                }

                                if (orderPrice <= biggestBidPrice)
                                {
                                    Log.Information("VolBot {0} safe run (bid) {1} {2}", bot.Base, orderPrice,
                                        biggestBidPrice);
                                    return;
                                }
                            }

                            if (orderPrice <= 0)
                            {
                                Log.Warning("orderPrice zero");
                                return;
                            }

                            if (orderQty < 0)
                            {
                                Log.Warning("orderQty zero");
                                return;
                            }

                            totalUsdVolume += orderQty * orderPrice;

                            const int orderWaitSecs = 0;
                            if (volumeOption.MatchingDelayFrom == 0 || volumeOption.MatchingDelayTo == 0)
                            {
                                var tasks = new List<Task>();

                                if (volumeOption.Side is OrderSide.SELL or OrderSide.BOTH)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL, orderWaitSecs));
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.BUY, 0));
                                }

                                if (volumeOption.Side is OrderSide.BUY)
                                {
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.BUY, orderWaitSecs));
                                    tasks.Add(CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL, 0));
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
                                        OrderSide.SELL, (int)bot.VolumeOptionObj.MatchingDelayTo + orderWaitSecs);

                                    await TradeDelay(bot);

                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.BUY, 0);
                                }

                                if (volumeOption.Side is OrderSide.BUY)
                                {
                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.BUY, (int)bot.VolumeOptionObj.MatchingDelayTo + orderWaitSecs);

                                    await TradeDelay(bot);

                                    await CreateLimitOrder(client, bot,
                                        orderQty.ToString($"F{basePrecision}", new NumberFormatInfo()),
                                        orderPrice.ToString($"F{quotePrecision}", new NumberFormatInfo()),
                                        OrderSide.SELL, 0);
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
            OrderSide side, int expireSecs)
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
            order.ExpiredTime = order.TransactTime + (int)TimeSpan.FromSeconds(expireSecs).TotalMilliseconds;

            order.Side = side.ToString();
            order.Type = bot.ExchangeType == BotExchangeType.COINSTORE ? "LIMIT" :
                string.IsNullOrEmpty(order.Type) ? "LIMIT" : order.Type;
            order.TransactTime = AppUtils.NowMilis();

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
            if (from > 1 && precision > 4)
                precision = 4;

            if (from >= to)
                return from;

            var roundPrecision = (int)Math.Pow(10, precision);

            return (decimal)new Random().Next((int)(from * roundPrecision), (int)(to * roundPrecision)) /
                   roundPrecision;
        }

        #endregion
    }
}