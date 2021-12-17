using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using sp.Core.Utils;

namespace sp.Core.Exchange
{
    public abstract class BaseExchange
    {
        protected readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private readonly Dictionary<string, ExchangeUsdPrice> _usdPriceResults =
            new Dictionary<string, ExchangeUsdPrice>();

        public async Task<decimal> GetUsdPrice(string coin)
        {
            coin = coin.ToUpper();

            try
            {
                var now = AppUtils.NowMilis();

                if (_usdPriceResults.TryGetValue(coin, out var result))
                {
                    if (result.LastUpdate + 2000 >= now)
                        return result.Price;
                }

                var usdPrice = await GetUsdPriceFromExchange(coin);

                if (result == null)
                {
                    result = new ExchangeUsdPrice
                    {
                        LastUpdate = now, Price = usdPrice
                    };

                    _usdPriceResults.TryAdd(coin, result);
                }
                else
                {
                    result.LastUpdate = now;
                    result.Price = usdPrice;
                }


                return result.Price;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        protected abstract Task<decimal> GetUsdPriceFromExchange(string coin);
    }

    public class ExchangeUsdPrice
    {
        public decimal Price { get; set; }

        public long LastUpdate { get; set; }
    }
}