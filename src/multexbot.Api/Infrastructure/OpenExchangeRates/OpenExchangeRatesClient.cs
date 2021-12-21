using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace multexbot.Api.Infrastructure.OpenExchangeRates
{
    public class OpenExchangeRatesClient
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<decimal> GetPrice(string fiat)
        {
            try
            {
                var url =
                    $@"https://openexchangerates.org/api/latest.json?app_id={Configurations.OpenExchangeRates.AppId}&base=USD&symbols={fiat}";

                var response =
                    await HttpClient
                        .GetAsync(url);

                var responseObj = JObject.Parse(await response.Content.ReadAsStringAsync());

                var stringPrice = responseObj["rates"][$"{fiat}"]?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(stringPrice))
                    return 0;

                var price = JsonConvert.DeserializeObject<decimal>(stringPrice);

                if (price == 0)
                    return 0;

                return price;
            }
            catch (Exception e)
            {
                Log.Warning(e, "OpenExchangeRatesClient:GetPrice");
                return 0m;
            }
        }
    }
}