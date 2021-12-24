using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using multexbot.Api.Constants;
using multexbot.Api.Infrastructure;
using multexbot.Api.Infrastructure.OpenExchangeRates;
using multexbot.Api.Models.Bot;
using multexbot.Api.Models.Market;
using multexbot.Api.Services.Interface;
using MySqlConnector;
using Newtonsoft.Json;
using Serilog;
using sp.Core.Exchange;
using sp.Core.Extensions;
using sp.Core.Utils;

namespace multexbot.Api.Services
{
    public class MarketService : IMarketService
    {
        private readonly BinanceExchange _binanceExchange;
        private readonly CoinbaseExchange _coinbaseExchange;
        private readonly HoubiExchange _houbiExchange;

        public MarketService()
        {
        }

        public MarketService(BinanceExchange binanceExchange,
            CoinbaseExchange coinbaseExchange,
            HoubiExchange houbiExchange)
        {
            _binanceExchange = binanceExchange;
            _coinbaseExchange = coinbaseExchange;
            _houbiExchange = houbiExchange;
        }

        #region Sys

        public async Task<MarketDto> SysGet(string coin)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var market = await dbConnection.QueryFirstOrDefaultAsync<MarketDto>(
                "SELECT * FROM Markets WHERE Coin = @Coin", new
                {
                    Coin = coin,
                });

            return market;
        }

        public async Task SysUpdatePrice()
        {
            var now = AppUtils.NowMilis();

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
            await sqlConnection.OpenAsync();

            var markets = await sqlConnection.QueryAsync<MarketDto>(
                "SELECT * FROM Markets WHERE IsActive = @IsActive",
                new
                {
                    IsActive = true
                });

            foreach (var market in markets)
            {
                try
                {
                    var price = 0m;

                    if (market.Coin == MultexBotConstants.KrwCoin)
                    {
                        //Follow OpenExchangeRates Get KRW
                        price = await OpenExchangeRatesClient.GetPrice(market.Coin);
                    }
                    else
                    {
                        //Follow Binance
                        price = await _binanceExchange.GetUsdPrice(market.Coin);

                        if (price <= 0)
                            price = await _coinbaseExchange.GetUsdPrice(market.Coin);

                        if (price <= 0)
                            price = await _houbiExchange.GetUsdPrice(market.Coin);
                    }

                    if (price == 0)
                    {
                        if (market.PriceUpdatedTime < AppUtils.NowMilis() -
                            TimeSpan.FromSeconds(MultexBotConstants.UpdateUsdPriceInterval * 3).TotalMilliseconds)
                        {
                            Log.Warning($"MultexBot:SysUpdatePrice {market.Coin}/USDT 0");
                            continue;
                        }
                    }

                    market.PriceUpdatedTime = now;
                    market.UsdPrice = price;

                    await sqlConnection.ExecuteAsync(
                        @"UPDATE Markets SET UsdPrice = @UsdPrice, PriceUpdatedTime = @PriceUpdatedTime WHERE Coin = @Coin",
                        market);

                    //Update Following Bot when price is change
                    await UpdateFollowingBot(market, sqlConnection);
                }
                catch (Exception e)
                {
                    Log.Error(e, "MultexBot SysUpdatePrice");
                }
            }
        }

        #endregion

        #region Private

        private async Task UpdateFollowingBot(MarketDto market, IDbConnection dbConnection)
        {
            var bots = (await dbConnection.QueryAsync<BotDto>(
                    "SELECT * FROM Bots WHERE Quote = @Quote",
                    new
                    {
                        Quote = market.Coin
                    }))
                .ToList();

            var followingBots = bots.Where(x => x.RootId.HasValue).ToList();

            var rootBots = bots.Where(x => !x.RootId.HasValue).ToList();

            #region Update By Root Bot

            foreach (var followingBot in followingBots)
            {
                try
                {
                    var rootBot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Id = @Id",
                        new
                        {
                            Id = followingBot.RootId
                        });

                    if (rootBot == null)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: Root Bot is null");
                        continue;
                    }

                    //Quote of Root Bot or Quote and Following Bot is not follow market
                    if (followingBot.Quote != market.Coin && rootBot.Quote != market.Coin)
                        continue;

                    if (rootBot.Base != followingBot.Base)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: Base of Root Bot is wrong");
                        continue;
                    }

                    await UpdateDatabase(rootBot, followingBot, dbConnection);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"UpdateFollowingBot by followingBotId={followingBot.Id}");
                }
            }

            #endregion

            #region Update By Following Bot

            foreach (var rootBot in rootBots)
            {
                try
                {
                    var followingBot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                        "SELECT * FROM Bots WHERE RootId = @RootId",
                        new
                        {
                            RootId = rootBot.Id
                        });

                    if (followingBot == null)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: Root Bot is null");
                        continue;
                    }

                    if (rootBot.Base != followingBot.Base)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: Base of Root Bot is wrong");
                        continue;
                    }

                    //Quote of Root Bot or Quote and Following Bot is not follow market
                    if (followingBot.Quote != market.Coin && rootBot.Quote != market.Coin)
                        continue;

                    await UpdateDatabase(rootBot, followingBot, dbConnection);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"UpdateFollowingBot by rootBotId={rootBot.Id}");
                }
            }

            #endregion
        }

        private async Task UpdateDatabase(BotDto rootBot, BotDto followingBot, IDbConnection dbConnection)
        {
            var options = JsonConvert.DeserializeObject<BotOption>(rootBot.Options);

            //Quote of Root Bot is stable coin
            if (followingBot.Quote != rootBot.Quote)
            {
                var followQuoteUsdPrice = 1m;
                var rootQuoteUsdPrice = 1m;

                if (!MultexBotConstants.StableCoins.Contains(followingBot.Quote))
                {
                    //Market for following bot
                    var followMarket = await SysGet(followingBot.Quote);

                    if (followMarket == null)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: {followMarket.Coin}/USDT 0");
                        return;
                    }

                    if (followMarket.UsdPrice == 0)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: {followMarket.Coin}/USDT 0");
                        return;
                    }

                    followQuoteUsdPrice = followMarket.UsdPrice;
                }

                if (!MultexBotConstants.StableCoins.Contains(rootBot.Quote))
                {
                    //Market for root bot
                    var rootMarket = await SysGet(rootBot.Quote);

                    if (rootMarket == null)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: {rootMarket.Coin}/USDT 0");
                        return;
                    }

                    if (rootMarket.UsdPrice == 0)
                    {
                        Log.Error("MultexBot UpdateFollowingBot: {rootMarket.Coin}/USDT 0");
                        return;
                    }

                    rootQuoteUsdPrice = rootMarket.UsdPrice;
                }

                options.BasePrice =
                    (options.BasePrice / followQuoteUsdPrice / rootQuoteUsdPrice).Truncate(options.PriceFix);
                options.FollowBtcBasePrice =
                    (options.FollowBtcBasePrice / followQuoteUsdPrice / rootQuoteUsdPrice).Truncate(options.PriceFix);
                options.MinStopPrice =
                    (options.MinStopPrice / followQuoteUsdPrice / rootQuoteUsdPrice).Truncate(options.PriceFix);
                options.MaxStopPrice =
                    (options.MaxStopPrice / followQuoteUsdPrice / rootQuoteUsdPrice).Truncate(options.PriceFix);
            }

            followingBot.Options = JsonConvert.SerializeObject(options);

            var exec = await dbConnection.ExecuteAsync(
                "UPDATE Bots SET Options = @Options WHERE Id = @Id AND UserId = @UserId",
                followingBot);

            if (exec == 0)
                Log.Error($"UpdateFollowingBot botId={followingBot.Id}");
        }

        #endregion
    }
}