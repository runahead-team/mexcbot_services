using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace sp.Core.Exchange
{
    public class OkexExchange : BaseExchange
    {
        protected override async Task<decimal> GetUsdPriceFromExchange(string coin)
        {
            var response =
                await HttpClient.GetStringAsync(
                    $"https://www.okex.com/api/spot/v3/instruments/{coin}-USDT/ticker");

            var responseObj = JObject.Parse(response);

            return decimal.Parse(responseObj["last"].ToString(), new CultureInfo("en-US"));
        }
    }
}