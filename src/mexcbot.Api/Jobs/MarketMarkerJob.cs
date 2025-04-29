using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
using sp.Core.Extensions;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class MarketMarkerJob : BackgroundService
    {
        private readonly IBotService _botService;

        public MarketMarkerJob(IBotService botService)
        {
            _botService = botService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>
            {
                CreateOrderJob(stoppingToken, BotExchangeType.MEXC),
                CreateOrderJob(stoppingToken, BotExchangeType.LBANK),
                CreateOrderJob(stoppingToken, BotExchangeType.COINSTORE),
                OrderbookBlinking(stoppingToken, BotExchangeType.MEXC),
                OrderbookBlinking(stoppingToken, BotExchangeType.LBANK),
                OrderbookBlinking(stoppingToken, BotExchangeType.COINSTORE)
            };

            await Task.WhenAll(tasks);
        }

        private async Task OrderbookBlinking(CancellationToken stoppingToken, BotExchangeType exchangeType)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND ExchangeType IN @ExchangeTypes AND IsRunBlinking = @IsRunBlinking",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.MAKER,
                            ExchangeTypes = new[]
                                { exchangeType },
                            IsRunBlinking = true
                        })).ToList();

                    if (bots.Count == 0)
                        continue;

                    var tasks = bots.Select(RunBlinking).ToList();

                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "MarketMarkerJob:OrderbookBlinking");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task CreateOrderJob(CancellationToken stoppingToken, BotExchangeType exchangeType)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND ExchangeType IN @ExchangeTypes AND (NextRunMakerTime < @Now OR NextRunMakerTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.MAKER,
                            ExchangeTypes = new[]
                                { exchangeType },
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    if (bots.Count == 0)
                        continue;

                    var tasks = bots.Select(RunMarketMaker).ToList();

                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "MarketMarkerJob:CreateOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task RunMarketMaker(BotDto bot)
        {
            var stopLog = "";
            var now = AppUtils.NowMilis();

            try
            {
                Log.Information("BOT {0} run", bot.Symbol);

                MemCache.AddActiveBot(bot);

                ExchangeClient client = bot.ExchangeType switch
                {
                    BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.COINSTORE => new CoinStoreClient(Configurations.CoinStoreUrl, bot.ApiKey,
                        bot.ApiSecret),
                    _ => null
                };

                if (client == null)
                    return;

                var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
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
                    Log.Error(e, "MarketMarkerJob: Update Bots {Id} {@data1} {@data2}", bot.Id, bot.ExchangeInfo,
                        bot.AccountInfo);
                }

                #endregion

                var makerOption = new BotMakerOption();
                var lastBtcPrice = 0m;

                #region Validate

                var baseBalanceValue = 0m;
                var quoteBalanceValue = 0m;

                if (bot.ExchangeType != BotExchangeType.COINSTORE)
                {
                    var selfSymbols = await client.GetSelfSymbols();
                    if (!selfSymbols.Contains(bot.Symbol))
                    {
                        bot.Status = BotStatus.INACTIVE;
                        stopLog += $"{bot.Symbol} is not support; \n";
                    }
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

                    //todo ANON
                    if (bot.Base == "ANON")
                    {
                    }
                    else if (makerOption.IsFollowBtc)
                    {
                        if (makerOption.FollowBtcBasePrice <= 0
                            || makerOption.FollowBtcBtcPrice <= 0)
                        {
                            bot.Status = BotStatus.INACTIVE;
                            stopLog += "Follow BTC price settings wrong; \n";
                        }

                        var lastBtcPriceStr = (await client.GetTicker24hr("BTC", "USDT")).LastPrice;

                        if (decimal.TryParse(lastBtcPriceStr, new NumberFormatInfo(), out var btcPrice))
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
                        var botLastPrice = decimal.Parse(bot24hr.LastPrice, new NumberFormatInfo());

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
                            var baseBalance = balances.FirstOrDefault(x =>
                                string.Equals(x.Asset, bot.Base, StringComparison.InvariantCultureIgnoreCase));

                            if (baseBalance == null)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Base} balance below 0 or null; \n";
                                Telegram.Send($"🟠 BOT {bot.Base} is stopped by balance {bot.Base} null");
                            }
                            else
                            {
                                if (decimal.TryParse(baseBalance.Free, new NumberFormatInfo(), out var value))
                                    baseBalanceValue = value;

                                if (baseBalanceValue <= 0)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog += $"Stop when your {bot.Base} balance below 0 or null; \n";
                                    Telegram.Send($"🟠 BOT {bot.Base} is stopped by balance {bot.Base} = 0");
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
                                            Telegram.Send(
                                                $"🟠 BOT {bot.Base} is stopped by balance {bot.Base}: {baseBalanceValue:N} < {makerOption.StopLossBase:N}");
                                        }
                                    }
                                }
                            }
                        }

                        if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.BUY)
                        {
                            var quoteBalance = balances.FirstOrDefault(x =>
                                string.Equals(x.Asset, bot.Quote, StringComparison.InvariantCultureIgnoreCase));

                            if (quoteBalance == null)
                            {
                                bot.Status = BotStatus.INACTIVE;
                                stopLog += $"Stop when your {bot.Quote} balance below 0 or null; \n";
                                Telegram.Send($"🟠 BOT {bot.Base} is stopped by balance {bot.Quote} null");
                            }
                            else
                            {
                                if (decimal.TryParse(quoteBalance.Free, new NumberFormatInfo(), out var value))
                                    quoteBalanceValue = value;

                                if (quoteBalanceValue <= 0)
                                {
                                    bot.Status = BotStatus.INACTIVE;
                                    stopLog += $"Stop when your {bot.Quote} balance below 0 or null; \n";
                                    Telegram.Send($"🟠 BOT {bot.Base} is stopped by balance {bot.Quote} = 0");
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
                                            Telegram.Send(
                                                $"🟠 BOT {bot.Base} is stopped by balance {bot.Quote}: {quoteBalanceValue:N} < {makerOption.StopLossQuote:N}");
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
                    var minQty = 0m;

                    if (!string.IsNullOrEmpty(exchangeInfo.MinQty))
                    {
                        if (bot.ExchangeType != BotExchangeType.COINSTORE)
                        {
                            minQty = (decimal.Parse(exchangeInfo.QuoteAmountPrecision, new NumberFormatInfo()) /
                                      decimal.Parse(bot24hr.LastPrice, new NumberFormatInfo()));
                        }
                        else
                        {
                            minQty = decimal.Parse(exchangeInfo.MinQty, new NumberFormatInfo());
                        }
                    }

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

                await UpdateBot(bot, false);

                #endregion

                if (exchangeInfo != null && !string.IsNullOrEmpty(bot.MakerOption))
                {
                    var price = 0m;

                    var quotePrecision = bot.QuotePrecision ?? exchangeInfo.QuoteAssetPrecision;
                    var basePrecision = bot.BasePrecision ?? exchangeInfo.BaseAssetPrecision;

                    var orderbook = (await client.GetOrderbook(bot.Base, bot.Quote));

                    if (orderbook == null || orderbook.Asks.Count == 0 || orderbook.Asks.Count == 0)
                    {
                        Log.Error("Order not found");
                        return;
                    }

                    if (orderbook.Asks.Count == 0 || orderbook.Bids.Count == 0)
                    {
                        Log.Error("Order not found");
                        return;
                    }

                    var maxPrice = orderbook.Asks.Min(x => x[0]);
                    var minPrice = orderbook.Bids.Max(x => x[0]);

                    if (minPrice == 0 || maxPrice == 0)
                        return;

                    var spread = maxPrice - minPrice;

                    await _botService.UpdateBotHistory(new BotHistoryDto
                    {
                        BotId = bot.Id,
                        Spread = spread,
                        BalanceBase = balances
                            .FirstOrDefault(x =>
                                string.Equals(x.Asset, bot.Base, StringComparison.InvariantCultureIgnoreCase))
                            ?.Free,
                        BalanceQuote = balances
                            .FirstOrDefault(x =>
                                string.Equals(x.Asset, bot.Quote, StringComparison.InvariantCultureIgnoreCase))
                            ?.Free
                    });

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

                                //todo ANON
                                if (bot.Base == "ANON")
                                {
                                    var httpClient = new HttpClient();

                                    var response =
                                        await httpClient.GetAsync(
                                            "https://api.huobi.pro/market/detail?symbol=anonusdt");

                                    var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());

                                    if (!decimal.TryParse(responseJson["tick"]?["close"]?.ToString(), out price))
                                    {
                                        Log.Error("ANON price error", bot.Symbol);
                                        return;
                                    }
                                }
                                else if (makerOption.IsFollowBtc)
                                {
                                    var change = 100 * (lastBtcPrice - makerOption.FollowBtcBtcPrice) /
                                                 makerOption.FollowBtcBtcPrice;

                                    if (makerOption.FollowBtcRate > 0)
                                        change *= makerOption.FollowBtcRate;

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

                                var qty = makerOption.IsRandomQty
                                    ? RandomNumber(makerOption.MinQty, makerOption.MinQty * 2, basePrecision)
                                    : RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision);

                                #region Trade

                                price = price.Truncate(quotePrecision);
                                qty = qty.Truncate(basePrecision);


                                var total = Math.Round(price * qty, 8);

                                if (makerOption.Side == OrderSide.BUY && quoteBalanceValue > total)
                                {
                                    await CreateLimitOrder(client, bot,
                                        qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.BUY, false);
                                }
                                else if (makerOption.Side == OrderSide.SELL && baseBalanceValue > qty)
                                {
                                    await CreateLimitOrder(client, bot,
                                        qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                        price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL);
                                }
                                else if (makerOption.Side == OrderSide.BOTH && baseBalanceValue > qty &&
                                         quoteBalanceValue > total)
                                {
                                    if (makerOption.MinMatchingTime == 0 &&
                                        makerOption.MaxMatchingTime == 0)
                                    {
                                        if (await CreateLimitOrder(client, bot,
                                                qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                                price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                                OrderSide.SELL))
                                        {
                                            await CreateLimitOrder(client, bot,
                                                qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                                price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                                OrderSide.BUY);
                                        }
                                    }
                                    else
                                    {
                                        if (await CreateLimitOrder(client, bot,
                                                qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                                price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                                OrderSide.SELL))
                                        {
                                            await TradeDelay(bot);

                                            await CreateLimitOrder(client, bot,
                                                qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                                                price.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                                OrderSide.BUY);
                                        }
                                    }
                                }

                                #endregion

                                #region Order Over Step

                                if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.BUY)
                                {
                                    if (makerOption.MinPriceOverStep < 0 && price > 0)
                                    {
                                        var overStepQty =
                                            RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision)
                                                .Truncate(basePrecision);

                                        var overStepPrice = RandomNumber(
                                            price + (makerOption.MinPriceStep + makerOption.MinPriceOverStep) * price /
                                            100,
                                            price + makerOption.MinPriceStep * price / 100, quotePrecision);

                                        if (overStepPrice > 0)
                                            await CreateLimitOrder(client, bot,
                                                overStepQty.ToString($"F{basePrecision.ToString()}",
                                                    new NumberFormatInfo()),
                                                overStepPrice.ToString($"F{quotePrecision.ToString()}",
                                                    new NumberFormatInfo()), OrderSide.BUY,
                                                true);
                                        await Task.Delay(TimeSpan.FromSeconds(1));
                                    }
                                }

                                if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.SELL)
                                {
                                    if (makerOption.MaxPriceOverStep > 0 && price > 0)
                                    {
                                        var overStepQty =
                                            RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision)
                                                .Truncate(basePrecision);

                                        var overStepPrice = RandomNumber(
                                            price + makerOption.MaxPriceOverStep *
                                            price / 100,
                                            price + (makerOption.MaxPriceStep + makerOption.MaxPriceOverStep) *
                                            price / 100, quotePrecision);

                                        if (overStepPrice > 0)
                                            await CreateLimitOrder(client, bot,
                                                overStepQty.ToString($"F{basePrecision.ToString()}",
                                                    new NumberFormatInfo()),
                                                overStepPrice.ToString($"F{quotePrecision.ToString()}",
                                                    new NumberFormatInfo()), OrderSide.SELL,
                                                true);
                                        await Task.Delay(TimeSpan.FromSeconds(1));
                                    }
                                }

                                #endregion
                            }, CancellationToken.None));
                        }

                        await Task.WhenAll(tasks);

                        #region Fill Orderbook

                        if (makerOption.Side == OrderSide.SELL)
                        {
                            price = minPrice;
                        }
                        else if (makerOption.Side == OrderSide.BUY)
                        {
                            price = maxPrice;
                        }
                        else
                        {
                            price = decimal.Parse(bot24hr.LastPrice, new CultureInfo("en-US"));
                        }

                        var minSpreadPerSide = (price * makerOption.Spread / 100) / 2;

                        if (price > 0)
                        {
                            var fillOrderBookPriceStep = 6 / (decimal)Math.Pow(10, quotePrecision);

                            if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.SELL)
                            {
                                var sellFromPrice = price * 1.02m;

                                var askList = orderbook.Asks
                                    .Where(x => x[0] < sellFromPrice)
                                    .ToList();
                                for (var sellPrice = sellFromPrice;
                                     sellPrice > price + minSpreadPerSide;
                                     sellPrice -= fillOrderBookPriceStep)
                                {
                                    sellPrice -= RandomNumber(0, 5, 0) / (decimal)Math.Pow(10, quotePrecision);

                                    if (askList.Any(x => x[0] < sellPrice))
                                        continue;

                                    var fillOrderBookQty =
                                        RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision)
                                            .Truncate(basePrecision);

                                    await CreateLimitOrder(client, bot,
                                        fillOrderBookQty.ToString($"F{basePrecision.ToString()}",
                                            new NumberFormatInfo()),
                                        sellPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.SELL, isFillOrder: true);
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                            }

                            if (makerOption.Side == OrderSide.BOTH || makerOption.Side == OrderSide.BUY)
                            {
                                var buyFromPrice = price * 0.98m;

                                var bidList = orderbook.Bids
                                    .Where(x => x[0] > buyFromPrice)
                                    .ToList();

                                for (var buyPrice = buyFromPrice;
                                     buyPrice < price - minSpreadPerSide;
                                     buyPrice += fillOrderBookPriceStep)
                                {
                                    buyPrice += RandomNumber(0, 5, 0) / (decimal)Math.Pow(10, quotePrecision);

                                    if (bidList.Any(x => x[0] > buyPrice))
                                        continue;

                                    var fillOrderBookQty =
                                        RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision)
                                            .Truncate(basePrecision);

                                    await CreateLimitOrder(client, bot,
                                        fillOrderBookQty.ToString($"F{basePrecision.ToString()}",
                                            new NumberFormatInfo()),
                                        buyPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                                        OrderSide.BUY, isFillOrder: true);
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                            }
                        }

                        #endregion
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

        private async Task RunBlinking(BotDto bot)
        {
            var stopLog = "";

            try
            {
                Log.Information("BOT {0} run", bot.Symbol);

                MemCache.AddActiveBot(bot);

                ExchangeClient client = bot.ExchangeType switch
                {
                    BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey, bot.ApiSecret),
                    BotExchangeType.COINSTORE => new CoinStoreClient(Configurations.CoinStoreUrl, bot.ApiKey,
                        bot.ApiSecret),
                    _ => null
                };

                if (client == null)
                    return;

                var bot24hr = await client.GetTicker24hr(bot.Base, bot.Quote);
                var exchangeInfo = bot.ExchangeInfoObj;
                var makerOption = JsonConvert.DeserializeObject<BotMakerOption>(bot.MakerOption);

                var botLastPrice = decimal.Parse(bot24hr.LastPrice, new NumberFormatInfo());

                var quotePrecision = bot.QuotePrecision ?? exchangeInfo.QuoteAssetPrecision;
                var basePrecision = bot.BasePrecision ?? exchangeInfo.BaseAssetPrecision;

                var qty = makerOption.IsRandomQty
                    ? RandomNumber(makerOption.MinQty, makerOption.MinQty * 2, basePrecision)
                    : RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision);

                if (new Random().Next(0, 2) == 0)
                {
                    var orderPrice = RandomNumber(botLastPrice * 0.98m, botLastPrice, quotePrecision);
                    await CreateLimitOrder(client, bot,
                        qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                        OrderSide.SELL);
                }
                else
                {
                    var orderPrice = RandomNumber(botLastPrice * 1.02m, botLastPrice, quotePrecision);

                    await CreateLimitOrder(client, bot,
                        qty.ToString($"F{basePrecision.ToString()}", new NumberFormatInfo()),
                        orderPrice.ToString($"F{quotePrecision.ToString()}", new NumberFormatInfo()),
                        OrderSide.BUY);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Bot run");
            }
        }

        #region Private

        private async Task<bool> CreateLimitOrder(ExchangeClient client, BotDto bot, string qty, string price,
            OrderSide side, bool isOverStepOrder = false, bool isFillOrder = false, bool isBlinking = false)
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

            order.BotId = bot.Id;
            order.BotType = bot.Type;
            order.BotExchangeType = bot.ExchangeType;
            order.UserId = bot.UserId;

            order.Side = side.ToString();
            order.Type = bot.ExchangeType == BotExchangeType.COINSTORE ? "LIMIT" : order.Type;
            order.TransactTime = AppUtils.NowMilis();

            if (isBlinking)
            {
                order.ExpiredTime =
                    order.TransactTime + 5000;
            }
            else if (isOverStepOrder && bot.MakerOptionObj != null && bot.MakerOptionObj.OrderExp > 0)
            {
                order.ExpiredTime =
                    order.TransactTime + (long)bot.MakerOptionObj.OrderExp * 1000;
            }
            else if (isFillOrder)
            {
                order.ExpiredTime = order.TransactTime + (long)TimeSpan
                    .FromMinutes(new Random().Next(120, 180)).TotalMilliseconds;
            }
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

                await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET Logs = @Logs, NextRunMakerTime = @NextRunMakerTime
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