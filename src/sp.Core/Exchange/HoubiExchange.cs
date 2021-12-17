using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class HoubiExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://www.bitstamp.net/api/v2/ticker/{coin.ToLower()}usd");

            var responseObj = JObject.Parse(response);

            return decimal.Parse(responseObj["last"].ToString(), new CultureInfo("en-US"));
        }
    }
}