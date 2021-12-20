using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace multexbot.Api.Infrastructure.Binance
{
    public class BinanceClient
    {
        private readonly HttpClient _httpClient;

        public BinanceClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<decimal> GetUsdPrice(string coin)
        {
            try
            {
                var symbol = $"{coin}USDT";

                var response =
                    await _httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol}");

                var ticker = JObject.Parse(response);

                return decimal.Parse(ticker["price"].ToString(), new CultureInfo("en-US"));
            }
            catch (Exception e)
            {
                Log.Fatal(e, "BinanceClient:GetUsdPrice");
                return 0;
            }
        }
    }
}