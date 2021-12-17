using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class BitstampExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://api.coinbase.com/v2/exchange-rates?currency={coin}");

            var responseObj = JObject.Parse(response);

            return decimal.Parse(responseObj["data"]["rates"]["USD"].ToString(), new CultureInfo("en-US"));
        }
    }
}