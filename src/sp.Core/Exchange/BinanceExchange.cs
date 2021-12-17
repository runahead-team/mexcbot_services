using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class BinanceExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://api.binance.com/api/v3/ticker/price?symbol={coin}USDT");

            var responseObj = JObject.Parse(response);

            return decimal.Parse(responseObj["price"].ToString(), new CultureInfo("en-US"));
        }
    }
}