using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DefaultNamespace;
using multexbot.Api.Constants;
using multexbot.Api.Infrastructure;
using multexbot.Api.Infrastructure.ExchangeClient;
using multexbot.Api.Models.Bot;
using multexbot.Api.Services.Interface;
using MySqlConnector;
using Newtonsoft.Json;
using Serilog;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Exchange;
using sp.Core.Extensions;
using sp.Core.Models;
using sp.Core.Utils;

namespace multexbot.Api.Services
{
    public class BotService : IBotService
    {
        private readonly BinanceExchange _binanceExchange;
        private readonly HoubiExchange _houbiExchange;
        private readonly CoinbaseExchange _coinbaseExchange;
        private readonly IMarketService _marketService;

        public BotService(BinanceExchange binanceExchange,
            HoubiExchange houbiExchange,
            CoinbaseExchange coinbaseExchange,
            IMarketService marketService)
        {
            _binanceExchange = binanceExchange;
            _houbiExchange = houbiExchange;
            _coinbaseExchange = coinbaseExchange;
            _marketService = marketService;
        }

        public async Task<List<BotView>> GetList(ExchangeType? exchangeType, AppUser user)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var bots = new List<BotDto>();

            if (exchangeType.HasValue)
            {
                bots = (await dbConnection.QueryAsync<BotDto>(
                    "SELECT * FROM Bots WHERE UserId = @UserId AND ExchangeType = @ExchangeType", new
                    {
                        UserId = user.Id,
                        ExchangeType = exchangeType
                    })).ToList();
            }
            else
            {
                bots = (await dbConnection.QueryAsync<BotDto>(
                    "SELECT * FROM Bots WHERE UserId = @UserId", new
                    {
                        UserId = user.Id
                    })).ToList();
            }

            if (!bots.Any())
                return new List<BotView>();

            return bots.Select(bot =>
            {
                var botView = new BotView(bot);

                #region Balance

                try
                {
                    BaseExchangeClient client = bot.ExchangeType switch
                    {
                        ExchangeType.FLATA => new FlataExchangeClient(Configurations.FlataUrl, bot.ApiKey,
                            bot.SecretKey.Decrypt(Configurations.HashKey)),
                        ExchangeType.SPEXCHANGE => new SpExchangeClient(Configurations.SpExchangeUrl, bot.ApiKey,
                            bot.SecretKey.Decrypt(Configurations.HashKey)),
                        ExchangeType.UPBIT => new UpbitExchangeClient(Configurations.UpbitUrl, bot.ApiKey,
                            bot.SecretKey.Decrypt(Configurations.HashKey)),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var balances = client.GetFunds(new string[] {bot.Base, bot.Quote}).Result;

                    if (balances.TryGetValue(bot.Base, out var baseBalance))
                    {
                        botView.BaseBalance = baseBalance;
                    }

                    if (balances.TryGetValue(bot.Quote, out var quoteBalance))
                    {
                        botView.QuoteBalance = quoteBalance;
                    }
                }
                catch (Exception)
                {
                    return botView;
                }

                #endregion

                return botView;
            }).ToList();
        }

        public async Task<BotView> Create(BotUpsertRequest request, AppUser user)
        {
            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
            await sqlConnection.OpenAsync();

            if (request.RootId.HasValue)
                request = await FollowRootBot(request, sqlConnection);

            var bot = new BotDto(request, user);
            bot.SecretKey = bot.SecretKey.Encrypt(Configurations.HashKey);

            #region Get Price

            BaseExchangeClient client = bot.ExchangeType switch
            {
                ExchangeType.FLATA => new FlataExchangeClient(Configurations.FlataUrl, bot.ApiKey,
                    bot.SecretKey.Decrypt(Configurations.HashKey)),
                ExchangeType.SPEXCHANGE => new SpExchangeClient(Configurations.SpExchangeUrl, bot.ApiKey,
                    bot.SecretKey.Decrypt(Configurations.HashKey)),
                ExchangeType.UPBIT => new UpbitExchangeClient(Configurations.UpbitUrl, bot.ApiKey,
                    bot.SecretKey.Decrypt(Configurations.HashKey)),
                _ => throw new ArgumentOutOfRangeException()
            };

            (bot.LastPrice, bot.LastPriceUsd, bot.OpenPrice) =
                await client.GetMarket(bot.Base, bot.Quote);

            #endregion

            bot.NextTime = AppUtils.NowMilis() + request.Options.MaxInterval;

            var exec = await sqlConnection.ExecuteAsync(
                @"INSERT INTO Bots(`Guid`,UserId,Email,`Name`,ExchangeType,ApiKey,SecretKey,Symbol,Base,Quote,Side,IsActive,LastExecute,NextTime,LastPrice,LastPriceUsd,Options,RootId)
                    VALUES(@Guid,@UserId,@Email,@Name,@ExchangeType,@ApiKey,@SecretKey,@Symbol,@Base,@Quote,@Side,@IsActive,@LastExecute,@NextTime,@LastPrice,@LastPriceUsd,@Options,@RootId)",
                bot);

            if (exec == 0)
                throw new AppException(AppError.UNKNOWN, "Insert Bot fail");

            bot.Id = await sqlConnection.QueryFirstOrDefaultAsync<int>("SELECT Id FROM Bots WHERE Guid = @Guid", new
            {
                Guid = bot.Guid
            });

            var botView = new BotView(bot);

            return botView;
        }

        public async Task Update(BotUpsertRequest request, AppUser user)
        {
            try
            {
                int exec = 0;

                await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
                await sqlConnection.OpenAsync();

                var oldBot = await sqlConnection.QueryFirstOrDefaultAsync<BotDto>(
                    "SELECT * FROM Bots WHERE Id = @Id AND UserId = @UserId",
                    new
                    {
                        Id = request.Id,
                        UserId = user.Id
                    });

                if (oldBot.RootId != request.RootId)
                    throw new AppException(AppError.UNKNOWN, "Can not update root");

                //Bot is a following bot
                if (oldBot.RootId.HasValue)
                    request = await FollowRootBot(request, sqlConnection);

                var bot = new BotDto(request, user);

                if (request.IsApiKeyChange)
                {
                    bot.SecretKey = bot.SecretKey.Encrypt(Configurations.HashKey);

                    exec = await sqlConnection.ExecuteAsync(
                        @"UPDATE Bots SET Name = @Name, Base = @Base, Quote = @Quote, ApiKey = @ApiKey, SecretKey = @SecretKey, Side = @Side, IsActive = @IsActive, Options = @Options WHERE Id = @Id AND UserId = @UserId",
                        bot);
                }
                else
                {
                    exec = await sqlConnection.ExecuteAsync(
                        @"UPDATE Bots SET Name = @Name, Base = @Base, Quote = @Quote, Side = @Side, IsActive = @IsActive, Options = @Options WHERE Id = @Id AND UserId = @UserId",
                        bot);
                }

                if (exec == 0)
                    throw new AppException(AppError.UNKNOWN, "Update Bot fail");

                //Bot is a root bot so update following bot
                if (!oldBot.RootId.HasValue)
                {
                    var followingBots = (await sqlConnection.QueryAsync<BotDto>(
                            "SELECT * FROM Bots WHERE RootId = @RootId AND UserId = @UserId",
                            new
                            {
                                RootId = bot.Id,
                                UserId = bot.UserId
                            }))
                        .ToList();

                    foreach (var followingBot in followingBots)
                    {
                        try
                        {
                            var newRequest = new BotUpsertRequest()
                            {
                                Id = followingBot.Id,
                                RootId = followingBot.RootId,
                                UserId = followingBot.UserId,
                                Base = followingBot.Base,
                                Quote = followingBot.Quote,
                                Symbol = followingBot.Symbol,
                                Options = JsonConvert.DeserializeObject<BotOption>(followingBot.Options)
                            };

                            #region Options follow root

                            newRequest.Options.BasePrice = request.Options.BasePrice;
                            newRequest.Options.FollowBtc = request.Options.FollowBtc;
                            newRequest.Options.FollowBtcBasePrice = request.Options.FollowBtcBasePrice;
                            newRequest.Options.FollowBtcBtcPrice = request.Options.FollowBtcBtcPrice;
                            newRequest.Options.LastPrice = request.Options.LastPrice;
                            newRequest.Options.MaxPriceStep = request.Options.MaxPriceStep;
                            newRequest.Options.MinPriceStep = request.Options.MinPriceStep;
                            newRequest.Options.MaxPriceOverStep = request.Options.MaxPriceOverStep;
                            newRequest.Options.MinPriceOverStep = request.Options.MinPriceOverStep;
                            newRequest.Options.MinStopPrice = request.Options.MinStopPrice;
                            newRequest.Options.MaxStopPrice = request.Options.MaxStopPrice;

                            #endregion
                            
                            newRequest = await FollowRootBot(newRequest, sqlConnection);

                            exec = await sqlConnection.ExecuteAsync(
                                @"UPDATE Bots SET Options = @Options WHERE Id = @Id AND UserId = @UserId",
                                new
                                {
                                    Id = newRequest.Id,
                                    UserId = newRequest.UserId,
                                    Options = JsonConvert.SerializeObject(newRequest.Options),
                                });

                            if (exec == 0)
                                Log.Error(
                                    $"Update following bots by id={newRequest.Id} rootId={newRequest.RootId} fail");
                        }
                        catch (Exception e)
                        {
                            Log.Error(e,
                                $"Update following bots by id={followingBot.Id} rootId={followingBot.RootId} fail");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Update Bot fail");
                throw new AppException(AppError.UNKNOWN, "Update Bot fail");
            }
        }

        public async Task Delete(long id, AppUser user)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var exec = await dbConnection.ExecuteAsync(
                @" DELETE FROM Bots WHERE Id = @Id AND UserId = @UserId", new
                {
                    Id = id,
                    UserId = user.Id
                });

            if (exec == 0)
                throw new AppException(AppError.UNKNOWN, "Delete bot fail");
        }

        #region Sys

        public async Task Run()
        {
            try
            {
                var now = AppUtils.NowMilis();

                List<BotDto> bots;
                await using (var sqlConnection = new MySqlConnection(Configurations.DbConnectionString))
                {
                    bots = (await sqlConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE IsActive = @IsActive AND NextTime <= @Now", new
                        {
                            IsActive = true,
                            Now = now,
                        })).ToList();
                }


                if (!bots.Any())
                    return;

                var tasks = new List<Task>();
                foreach (var bot in bots)
                {
                    if (bot.ExchangeType == ExchangeType.SPEXCHANGE)
                        tasks.Add(Run<SpExchangeClient>(bot));

                    if (bot.ExchangeType == ExchangeType.UPBIT)
                        tasks.Add(Run<UpbitExchangeClient>(bot));

                    if (bot.ExchangeType == ExchangeType.FLATA)
                        tasks.Add(Run<FlataExchangeClient>(bot));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Log.Error(e, "Bot run");
            }
        }

        public async Task CancelExpiredOrder()
        {
            List<OrderDto> orders;
            await using (var sqlConnection = new MySqlConnection(Configurations.DbConnectionString))
            {
                await sqlConnection.OpenAsync();

                orders = (await sqlConnection.QueryAsync<OrderDto>(
                    "SELECT o.*, b.ApiKey, b.SecretKey, b.ExchangeType FROM BotOrders o LEFT JOIN Bots b ON o.BotId = b.Id WHERE o.ExpiredTime > 0 && o.ExpiredTime <= @Now AND o.IsExpired = @IsExpired",
                    new
                    {
                        Now = AppUtils.NowMilis(),
                        IsExpired = false
                    })).ToList();

                if (!orders.Any())
                    return;

                var ids = orders.Select(x => x.Id).ToList();

                var exec = await sqlConnection.ExecuteAsync(
                    "DELETE FROM BotOrders WHERE Id IN @Ids", new
                    {
                        IsExpired = true,
                        Ids = ids
                    });

                if (exec == 0)
                    Log.Error("Bot update order expired fail");
            }

            var tasks = new List<Task>();

            foreach (var order in orders)
            {
                tasks.Add(Task.Run(async () =>
                {
                    BaseExchangeClient client = order.ExchangeType switch
                    {
                        ExchangeType.FLATA => new FlataExchangeClient(Configurations.FlataUrl, order.ApiKey,
                            order.SecretKey.Decrypt(Configurations.HashKey)),
                        ExchangeType.SPEXCHANGE => new SpExchangeClient(Configurations.SpExchangeUrl, order.ApiKey,
                            order.SecretKey.Decrypt(Configurations.HashKey)),
                        ExchangeType.UPBIT => new UpbitExchangeClient(Configurations.UpbitUrl, order.ApiKey,
                            order.SecretKey.Decrypt(Configurations.HashKey)),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    if (await client.Cancel(order.ExternalId.ToString(), null))
                    {
                        Log.Information("Bot cancel order {0} {1} {2} {3}", nameof(BaseExchangeClient),
                            order.ExchangeType, order.Id,
                            order.ExternalId);
                    }
                    else
                    {
                        Log.Information("Bot cancel order fail {0} {1} {2} {3}", nameof(BaseExchangeClient),
                            order.ExchangeType,
                            order.Id,
                            order.ExternalId);
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        public async Task ClearOrderJob()
        {
            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
            await sqlConnection.OpenAsync();

            await sqlConnection.ExecuteAsync(
                "DELETE FROM BotOrders WHERE ExpiredTime = 0 || ExpiredTime <= @Now || IsExpired = @IsExpired", new
                {
                    Now = AppUtils.NowMilis() - TimeSpan.FromMinutes(5).TotalMilliseconds,
                    IsExpired = true
                });
        }

        #endregion

        #region Private

        private async Task Run<T>(BotDto bot) where T : BaseExchangeClient
        {
            try
            {
                var stopLog = "";

                var options = JsonConvert.DeserializeObject<BotOption>(bot.Options);

                bot.OrderExp = options.OrderExp;

                var now = AppUtils.NowMilis();

                Log.Information("BOT {0} run", bot.Symbol);

                var url = bot.ExchangeType switch
                {
                    ExchangeType.FLATA => Configurations.FlataUrl,
                    ExchangeType.UPBIT => Configurations.UpbitUrl,
                    ExchangeType.SPEXCHANGE => Configurations.SpExchangeUrl,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var client = (T) Activator.CreateInstance(typeof(T), url, bot.ApiKey,
                    bot.SecretKey.Decrypt(Configurations.HashKey));

                if (client == null)
                    return;

                (bot.LastPrice, bot.LastPriceUsd, bot.OpenPrice) = await client.GetMarket(bot.Base, bot.Quote);

                #region Validate

                if (bot.LastPrice == 0 || bot.LastPriceUsd == 0)
                    return;

                if (options.MinStopPrice < 0 && bot.LastPrice <= options.MinStopPrice)
                {
                    bot.IsActive = false;
                    stopLog += $"Stop when price cross down {options.MinStopPrice}\n";
                }

                if (options.MaxStopPrice > 0 && bot.LastPrice >= options.MaxStopPrice)
                {
                    bot.IsActive = false;
                    stopLog += $"Stop when price cross up {options.MaxStopPrice}\n";
                }

                var balances = await client.GetFunds(bot.Base, bot.Quote);

                if (!balances.Any())
                    return;

                if (balances.TryGetValue(bot.Base, out var baseBalance))
                {
                    if (options.StopLossBase > 0)
                    {
                        if (baseBalance <= options.StopLossBase)
                        {
                            bot.IsActive = false;
                            stopLog += $"Stop when your {bot.Base} balance lower than {options.StopLossBase}\n";
                        }
                    }
                }
                else
                {
                    bot.IsActive = false;
                    stopLog += $"Get {bot.Base} balance error\n";
                }


                if (balances.TryGetValue(bot.Quote, out var quoteBalance))
                {
                    if (options.StopLossQuote > 0)
                    {
                        if (quoteBalance <= options.StopLossQuote)
                        {
                            bot.IsActive = false;
                            stopLog += $"Stop when your {bot.Quote} balance lower than {options.StopLossQuote}\n";
                        }
                    }
                }
                else
                {
                    bot.IsActive = false;
                    stopLog += $"Get {bot.Quote} balance error \n";
                }

                decimal lastBtcPrice = 0;

                if (options.FollowBtc)
                {
                    if (!AppConstants.UsdStableCoins.Contains(bot.Quote))
                    {
                        bot.IsActive = false;
                        stopLog += $"{bot.Symbol} not allow follow BTC price\n";
                    }

                    if (options.FollowBtcBasePrice <= 0
                        || options.FollowBtcBtcPrice <= 0)
                    {
                        bot.IsActive = false;
                        stopLog += "Follow BTC price settings wrong\n";
                    }

                    lastBtcPrice = await _binanceExchange.GetUsdPrice("BTC");

                    if (lastBtcPrice <= 0)
                        lastBtcPrice = await _houbiExchange.GetUsdPrice("BTC");

                    if (lastBtcPrice <= 0)
                        lastBtcPrice = await _coinbaseExchange.GetUsdPrice("BTC");

                    if (lastBtcPrice <= 0)
                    {
                        Log.Error("BOT {0} get BTC price error", bot.Symbol);
                        return;
                    }

                    if (options.MinPriceStep == 0)
                        options.MinPriceStep = -0.15m;

                    if (options.MaxPriceStep == 0)
                        options.MaxPriceStep = 0.15m;
                }

                #endregion

                //Stop Log
                bot.Log = stopLog;

                if (bot.IsActive)
                {
                    bot.NextTime = now + (int) RandomNumber(options.MinInterval, options.MaxInterval) * 1000;

                    decimal price = 0;

                    var orderbook = await client.GetOrderbook(bot.Base, bot.Quote);

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
                        var numOfTrades = (int) RandomNumber(options.MinTradePerExec, options.MaxTradePerExec);

                        var tasks = new List<Task>();

                        for (var i = 0; i < numOfTrades; i++)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                #region Price

                                if (options.FollowBtc)
                                {
                                    var change = 100 * (lastBtcPrice - options.FollowBtcBtcPrice) /
                                                 options.FollowBtcBtcPrice;

                                    change = change * 0.5m;

                                    price = RandomNumber(
                                        options.FollowBtcBasePrice + options.FollowBtcBasePrice *
                                        (change + options.MinPriceStep) / 100,
                                        options.FollowBtcBasePrice + options.FollowBtcBasePrice *
                                        (change + options.MaxPriceStep) / 100);
                                }
                                else
                                {
                                    if (spreadHigh)
                                    {
                                        //SELL
                                        price = RandomNumber(maxPrice * (1 - spreadFixPercent / 100), maxPrice);
                                    }
                                    else
                                    {
                                        if (options.LastPrice)
                                        {
                                            price = RandomNumber(
                                                bot.LastPrice + options.MinPriceStep * bot.LastPrice / 100,
                                                bot.LastPrice + options.MaxPriceStep * bot.LastPrice / 100);
                                        }
                                        else if (options.BasePrice > 0)
                                        {
                                            price = RandomNumber(
                                                options.BasePrice + options.MinPriceStep * options.BasePrice / 100,
                                                options.BasePrice + options.MaxPriceStep * options.BasePrice / 100);
                                        }
                                        else
                                        {
                                            return;
                                        }

                                        if (price > maxPrice)
                                            price = maxPrice;
                                        else if (price < minPrice)
                                            price = minPrice;
                                    }
                                }

                                if (price <= 0)
                                {
                                    Log.Warning("BOT {0} price=0", bot.Symbol);
                                    return;
                                }

                                #endregion

                                #region Qty

                                decimal qty;

                                if (options.RandomQty)
                                {
                                    qty = RandomNumber(options.MinQty, options.MinQty * 2);
                                }
                                else
                                {
                                    qty = RandomNumber(options.MinQty, options.MaxQty);
                                }

                                #endregion

                                #region Trade

                                price = price.Truncate(options.PriceFix);
                                qty = qty.Truncate(options.QtyFix);

                                var total = Math.Round(price * qty, 8);

                                if (bot.Side == OrderSide.BUY && quoteBalance > total)
                                {
                                    await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                }
                                else if (bot.Side == OrderSide.SELL && baseBalance > qty)
                                {
                                    await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);
                                }
                                else if (bot.Side == OrderSide.BOTH && baseBalance > qty && quoteBalance > total)
                                {
                                    if (spreadHigh)
                                    {
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);

                                        // var matchPrice = RandomNumber(minPrice, minPrice * 1.01m);
                                        // await CreateLimitOrder(client, bot, qty, matchPrice, OrderSide.SELL);
                                        // await TradeDelay(options);
                                        // await CreateLimitOrder(client, bot, qty, matchPrice, OrderSide.BUY);
                                    }

                                    if (options.MinMatchingTime == 0 &&
                                        options.MaxMatchingTime == 0)
                                    {
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                    }
                                    else
                                    {
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);
                                        await TradeDelay(options);
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                    }
                                }

                                #endregion

                                #region Order Over Step

                                if (options.MinPriceOverStep < 0)
                                {
                                    if (options.LastPrice)
                                    {
                                        price = RandomNumber(
                                            bot.LastPrice + (options.MinPriceStep + options.MinPriceOverStep) *
                                            bot.LastPrice / 100,
                                            bot.LastPrice + options.MinPriceStep * bot.LastPrice / 100);
                                    }
                                    else if (options.BasePrice > 0)
                                    {
                                        price = RandomNumber(
                                            options.BasePrice + (options.MinPriceStep + options.MinPriceOverStep) *
                                            options.BasePrice / 100,
                                            options.BasePrice + options.MaxPriceStep * options.BasePrice / 100);
                                    }
                                    else
                                    {
                                        price = 0;
                                    }

                                    price = price.Truncate(options.PriceFix);

                                    if (price > 0)
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                }

                                if (options.MaxPriceOverStep > 0)
                                {
                                    if (options.LastPrice)
                                    {
                                        price = RandomNumber(
                                            bot.LastPrice + options.MaxPriceOverStep *
                                            bot.LastPrice / 100,
                                            bot.LastPrice + (options.MaxPriceStep + options.MaxPriceOverStep) *
                                            bot.LastPrice / 100);
                                    }
                                    else if (options.BasePrice > 0)
                                    {
                                        price = RandomNumber(
                                            options.BasePrice + options.MaxPriceOverStep *
                                            options.BasePrice / 100,
                                            options.BasePrice + (options.MaxPriceStep + options.MaxPriceOverStep) *
                                            options.BasePrice / 100);
                                    }
                                    else
                                    {
                                        price = 0;
                                    }

                                    price = price.Truncate(options.PriceFix);

                                    if (price > 0)
                                        await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL);
                                }

                                #endregion

                                #region BTC Spread

                                if (options.FollowBtc && spreadHigh)
                                {
                                    //Buy more 
                                    if (price >= maxPrice)
                                    {
                                        var buyPrice = minPrice * (1 + spreadFixPercent / 100);
                                        buyPrice = buyPrice.Truncate(options.PriceFix);
                                        await CreateLimitOrder(client, bot, qty / 2, buyPrice, OrderSide.BUY);
                                    }
                                    //Sell more 
                                    else if (price <= minPrice)
                                    {
                                        var sellPrice = maxPrice * (1 - spreadFixPercent / 100);
                                        sellPrice = sellPrice.Truncate(options.PriceFix);
                                        await CreateLimitOrder(client, bot, qty / 2, sellPrice, OrderSide.SELL);
                                    }
                                }

                                #endregion
                            }, CancellationToken.None));
                        }

                        await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "MmBot trade");
                    }
                }

                await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

                int exec;

                if (!bot.IsActive)
                {
                    exec = await sqlConnection.ExecuteAsync(
                        "UPDATE Bots SET IsActive = @IsActive, NextTime = @NextTime, LastPrice = @LastPrice, LastPriceUsd = @LastPriceUsd, Log = @Log WHERE Id = @Id",
                        bot);
                }
                else
                {
                    exec = await sqlConnection.ExecuteAsync(
                        "UPDATE Bots SET NextTime = @NextTime, LastPrice = @LastPrice, LastPriceUsd = @LastPriceUsd WHERE Id = @Id",
                        bot);
                }

                if (exec == 0)
                    Log.Error("Bot update NextTime fail {0}", bot.Id);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Bot run inner");
            }
        }

        private async Task TradeDelay(BotOption option)
        {
            await Task.Delay((int) RandomNumber(option.MinMatchingTime,
                option.MaxMatchingTime) * 1000);
        }

        private async Task<bool> CreateLimitOrder<T>(T client, BotDto bot, decimal qty, decimal price, OrderSide side)
            where T : BaseExchangeClient
        {
            var order = await client.CreateLimitOrder(bot.Base, bot.Quote, qty, price, side);

            if (order == null)
                return false;

            Log.Information("Bot create order {0} {1}", typeof(T).Name,
                $"{side} {qty.ToCurrencyString()} {bot.Symbol} at price {price.ToCurrencyString()} {order.ExternalId}");

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

            order.Guid = AppUtils.NewGuidStr();
            order.BotId = bot.Id;
            order.UserId = bot.UserId;
            order.Time = AppUtils.NowMilis();
            order.ExpiredTime = bot.OrderExp > 0 ? order.Time + bot.OrderExp * 1000 : 0;
            order.IsExpired = false;

            var exec = await sqlConnection.ExecuteAsync(
                @"INSERT INTO BotOrders(Guid,BotId,UserId,ExternalId,Symbol,Base,Quote,Side,Price,Qty,Total,ExpiredTime,IsExpired,`Time`)
                      VALUES(@Guid,@BotId,@UserId,@ExternalId,@Symbol,@Base,@Quote,@Side,@Price,@Qty,@Total,@ExpiredTime,@IsExpired,@Time)",
                order);

            if (exec == 0)
                Log.Error("Bot insert order fail {0} {@data}", typeof(T).Name, order);

            return true;
        }

        private decimal RandomNumber(decimal from, decimal to)
        {
            if (from >= to)
                return from;

            int round;

            if (from > 10)
            {
                round = 1000;
            }
            else if (from > 1)
            {
                round = 100000;
            }
            else if (from > 0.1m)
            {
                round = 1000000;
            }
            else if (from > 0.01m)
            {
                round = 10000000;
            }
            else
            {
                round = 100000000;
            }

            return (decimal) new Random().Next((int) (from * round), (int) (to * round)) / round;
        }

        private async Task<BotUpsertRequest> FollowRootBot(BotUpsertRequest request, IDbConnection sqlConnection)
        {
            var rootBot = await sqlConnection.QueryFirstOrDefaultAsync<BotDto>(
                "SELECT * FROM Bots WHERE Id = @Id",
                new
                {
                    Id = request.RootId
                });

            if (rootBot == null)
                throw new AppException(AppError.UNKNOWN, "Root Bot is null");

            if (rootBot.Base != request.Base)
                throw new AppException(AppError.UNKNOWN, "Base of Root Bot is wrong");

            var options = JsonConvert.DeserializeObject<BotOption>(rootBot.Options);

            //Quote of Root Bot is stable coin
            if (request.Quote != rootBot.Quote)
            {
                var followQuoteUsdPrice = 1m;
                var rootQuoteUsdPrice = 1m;

                if (!MultexBotConstants.StableCoins.Contains(request.Quote))
                {
                    //Market for following bot
                    var followMarket = await _marketService.SysGet(request.Quote);

                    if (followMarket == null)
                        throw new AppException(AppError.MARKET_IS_NOT_EXIST);

                    if (followMarket.UsdPrice == 0)
                        throw new AppException(AppError.UNKNOWN, $"MultexBot {followMarket.Coin}/USDT 0");

                    followQuoteUsdPrice = followMarket.UsdPrice;
                }

                if (!MultexBotConstants.StableCoins.Contains(rootBot.Quote))
                {
                    //Market for root bot
                    var rootMarket = await _marketService.SysGet(rootBot.Quote);

                    if (rootMarket == null)
                        throw new AppException(AppError.MARKET_IS_NOT_EXIST);

                    if (rootMarket.UsdPrice == 0)
                        throw new AppException(AppError.UNKNOWN, $"MultexBot {rootMarket.Coin}/USDT 0");

                    rootQuoteUsdPrice = rootMarket.UsdPrice;
                }

                options.BasePrice /= (followQuoteUsdPrice / rootQuoteUsdPrice);
                options.FollowBtcBasePrice /= (followQuoteUsdPrice / rootQuoteUsdPrice);
                options.MinStopPrice /= (followQuoteUsdPrice / rootQuoteUsdPrice);
                options.MaxStopPrice /= (followQuoteUsdPrice / rootQuoteUsdPrice);
            }

            request.Options.BasePrice = options.BasePrice.Truncate(options.PriceFix);
            request.Options.FollowBtc = options.FollowBtc;
            request.Options.FollowBtcBasePrice = options.FollowBtcBasePrice.Truncate(options.PriceFix);
            request.Options.FollowBtcBtcPrice = options.FollowBtcBtcPrice;
            request.Options.LastPrice = options.LastPrice;
            request.Options.MaxPriceStep = options.MaxPriceStep;
            request.Options.MinPriceStep = options.MinPriceStep;
            request.Options.MaxPriceOverStep = options.MaxPriceOverStep;
            request.Options.MinPriceOverStep = options.MinPriceOverStep;
            request.Options.MinStopPrice = options.MinStopPrice.Truncate(options.PriceFix);
            request.Options.MaxStopPrice = options.MaxStopPrice.Truncate(options.PriceFix);

            return request;
        }

        #endregion
    }
}