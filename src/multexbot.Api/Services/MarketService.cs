using System;
using System.Threading.Tasks;
using Dapper;
using multexbot.Api.Constants;
using multexbot.Api.Infrastructure;
using multexbot.Api.Infrastructure.OpenExchangeRates;
using multexbot.Api.Models.Market;
using multexbot.Api.Services.Interface;
using MySqlConnector;
using Serilog;
using sp.Core.Exchange;
using sp.Core.Models;
using sp.Core.Utils;

namespace multexbot.Api.Services
{
    public class MarketService : IMarketService
    {
        private readonly BinanceExchange _binanceExchange;
        
        public MarketService(){}

        public MarketService(BinanceExchange binanceExchange)
        {
            _binanceExchange = binanceExchange;
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
                    }
                    
                    if (price == 0)
                    {
                        if (market.PriceUpdatedTime < AppUtils.NowMilis() -
                            TimeSpan.FromSeconds(MultexBotConstants.UpdateUsdPriceInterval * 3).TotalMilliseconds)
                        {
                            Log.Warning($"MultexBot {market.Coin}/USDT 0");
                            continue;
                        }
                    }

                    market.PriceUpdatedTime = now;
                    market.UsdPrice = price;
                    
                    await sqlConnection.ExecuteAsync(
                        @"UPDATE Markets SET UsdPrice = @UsdPrice, PriceUpdatedTime = @PriceUpdatedTime WHERE Coin = @Coin",
                        market);
                }
                catch (Exception e)
                {
                    Log.Error(e, "MultexBot SysUpdatePrice");
                }
            }
        }
        
        #endregion

        #region Private

        private void Filter(SqlBuilder builder, TableRequest request)
        {
            if (request.Filters.TryGetValue("coin", out var coin) && !string.IsNullOrEmpty(coin))
            {
                builder.Where("m.Coin = @Coin", new {Coin = coin});
            }
        }

        #endregion
    }
}