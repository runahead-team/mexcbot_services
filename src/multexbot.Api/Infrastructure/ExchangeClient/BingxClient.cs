using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DefaultNamespace;
using multexbot.Api.Constants;
using multexbot.Api.Infrastructure.ExchangeClient;
using multexBot.Api.Models.Bingx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Utils;

namespace multexBot.Api.Infrastructure.ExchangeClient
{
    public class BingxClient : BaseExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;

        private readonly HttpClient _httpClient = new HttpClient();

        public BingxClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public BingxClient(string baseUrl, string apiKey, string secretKey)
        {
            _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", apiKey);

            _apiKey = apiKey;
            _baseUri = new Uri(baseUrl);
            _secretKey = secretKey;
        }

        public override async Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(string @base,
            string quote)
        {
            var (success, responseBody) =
                await SendRequest("GET",
                    $"https://api-swap-rest.bingbon.pro/api/v1/market/getTicker?symbol={@base}-{quote}");

            if (!success)
                return (0, 0, 0);

            var ticker = JObject.Parse(responseBody)["data"]?["tickers"]?[0];

            return (
                decimal.Parse(ticker["tradePrice"].ToString()),
                decimal.Parse(ticker["tradePrice"].ToString()),
                decimal.Parse(ticker["openPrice"].ToString())
            );
        }

        public override async Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side)
        {
            var payload = $"symbol={@base}-{quote}&side={side:G}&type=LIMIT&price={price}&quantity={amount}";

            var (success, responseBody) =
                await SendRequest("POST", $"/openApi/spot/v1/trade/order?{payload}");

            if (!success)
                return null;

            var order = JObject.Parse(responseBody)["data"];

            var orderDto = new OrderDto
            {
                Base = @base,
                Quote = quote,
                Qty = amount,
                Price = price,
                Side = side,
                ExternalId = long.Parse(order["orderId"].ToString())
            };

            return orderDto;
        }

        public override async Task<List<OrderDto>> GetOpenOrders(string @base, string quote)
        {
            var (success, responseBody) =
                await SendRequest("GET", $" /openApi/spot/v1/trade/openOrders?symbol={@base}-{quote}");

            if (!success)
                return new List<OrderDto>();

            var orders = JObject.Parse(responseBody)["data"]?["orders"];

            var returnOrders = new List<OrderDto>();

            foreach (var order in orders)
            {
                returnOrders.Add(new OrderDto
                {
                    ExternalId = (long) order["orderId"],
                    Price = decimal.Parse(order["price"].ToString()),
                    Qty = decimal.Parse(order["origQty"].ToString()),
                    Filled = decimal.Parse(order["executedQty"].ToString()),
                });
            }

            return returnOrders;
        }

        public override async Task<bool> Cancel(string id, string @base = null, string quote = null)
        {
            var payload = $"symbol={@base}-{quote}&orderId={id}";

            var (success, responseBody) =
                await SendRequest("POST", $"/openApi/spot/v1/trade/cancel?{payload}");

            return success;
        }

        public override async Task<Dictionary<string, decimal>> GetFunds(params string[] coins)
        {
            var (success, responseBody) =
                await SendRequest("POST", $"/openApi/spot/v1/account/balance");

            var returnBalances = new Dictionary<string, decimal>();
            if (!success)
                return returnBalances;

            var balances = JObject.Parse(responseBody)["data"]?["balances"];

            foreach (var coin in coins)
            {
                foreach (var balance in balances)
                {
                    if (string.Equals(balance["asset"].ToString(), coin, StringComparison.CurrentCultureIgnoreCase))
                    {
                        returnBalances.Add(coin, decimal.Parse(balance["free"].ToString()));
                        break;
                    }
                }
            }

            return returnBalances;
        }

        public override async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var (success, responseBody) =
                await SendRequest("GET", $"/openApi/spot/v1/market/depth?symbol={@base}-{quote}");

            if (!success)
                return new OrderbookView();

            var data = JObject.Parse(responseBody)["data"];

            return JsonConvert.DeserializeObject<OrderbookView>(data.ToString());
        }

        private async Task<(bool, string)> SendRequest(string method, string endpoint, string payload = "",
            bool useSignature = false, bool logInfo = true)
        {
            var uri =
                endpoint.StartsWith("https")
                    ? new Uri(endpoint)
                    : new Uri(_baseUri, endpoint);

            if (logInfo)
                Log.Information($"BingxClient:SendRequest request {endpoint} {payload}");

            if (useSignature)
            {
                if (!string.IsNullOrEmpty(payload))
                {
                    var timestamp = AppUtils.NowMilis();
                    payload = $"{payload}&timestamp={timestamp}";
                    var signature = AppUtils.HMAC_SHA256(payload, _secretKey);
                    payload = $"{payload}&signature={signature}";
                }
                else
                {
                    var timestamp = AppUtils.NowMilis();
                    payload = $"timestamp={timestamp}";
                    var signature = AppUtils.HMAC_SHA256(payload, _secretKey);
                    payload = $"{payload}&signature={signature}";
                }
            }

            string responseBody = null;

            try
            {
                HttpResponseMessage response;

                switch (method)
                {
                    case "GET":
                        response = await _httpClient.GetAsync($"{uri}?{payload}");
                        break;
                    case "POST":
                        response = await _httpClient.PostAsync(uri, new StringContent(payload));
                        break;
                    case "PUT":
                        response = await _httpClient.PutAsync(uri, new StringContent(payload));
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync($"{uri}?{payload}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                if (response.StatusCode == HttpStatusCode.OK)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                    if (logInfo)
                        Log.Information($"BingxClient:SendRequest response {endpoint} {payload} {responseBody}");
                    return (true, responseBody);
                }

                responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<BingxError>(responseBody);

                Log.Error($"BingxClient:SendRequest response {endpoint} {payload} {responseBody}");

                var errorMessage = $"{error.Code} {error.Msg}";

                return (false, errorMessage);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Log.Error("BingxClient request timeout {uri} {response}", uri, responseBody);
                else if (e is HttpRequestException)
                    Log.Error("BingxClient http error {uri} {response}", uri, responseBody);
                else
                    Log.Error(e, "BingxClient {response}", responseBody);

                return (false, string.Empty);
            }
        }
    }
}