using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class BitfinexExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://api-pub.bitfinex.com/v2/ticker/t{coin}USD");

            var responseObj = JArray.Parse(response);

            return decimal.Parse(responseObj[6].ToString(), new CultureInfo("en-US"));
        }
    }
}