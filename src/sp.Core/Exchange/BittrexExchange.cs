using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class BittrexExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://api.bittrex.com/api/v1.1/public/getticker?market=USD-{coin}");

            var responseObj = JObject.Parse(response);

            return decimal.Parse(responseObj["result"]["Last"].ToString(), new CultureInfo("en-US"));
        }
    }
}