using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Serilog;
using Newtonsoft.Json.Linq;
using sp.Core.Utils;

namespace mexcbot.Api.Infrastructure.ExchangeClient
{
    public class MexcClient : ExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;

        private readonly HttpClient _httpClient = new HttpClient();

        public MexcClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public MexcClient(string baseUrl, string apiKey, string secretKey)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-mexc-apikey", apiKey);
            _baseUri = new Uri(baseUrl);

            _apiKey = apiKey;
            _secretKey = secretKey;
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var payload = $"symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/exchangeInfo", payload, false, false);

            if (!success)
                return new ExchangeInfoView();

            var data = JObject.Parse(responseBody)["symbols"][0];

            return data != null
                ? JsonConvert.DeserializeObject<ExchangeInfoView>(data.ToString())
                : new ExchangeInfoView();
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            var payload = $"symbol={@base}{quote}&interval={interval}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/klines", payload, false, false);

            if (!success)
                return new List<JArray>();

            return JsonConvert.DeserializeObject<List<JArray>>(responseBody);
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var payload = $"symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/ticker/24hr", payload, false, false);

            if (!success)
                return new Ticker24hrView();

            var data = JObject.Parse(responseBody);

            return JsonConvert.DeserializeObject<Ticker24hrView>(data.ToString());
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var payload = $"symbol={@base}{quote}&side={side}&type={OrderType.LIMIT}&quantity={quantity}&price={price}";

            var (success, responseBody) =
                await SendRequest("POST", "/api/v3/order", payload, true, true);

            if (!success)
                return new OrderDto();

            var data = JObject.Parse(responseBody);

            return JsonConvert.DeserializeObject<OrderDto>(data.ToString());
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            var payload = $"symbol={@base}{quote}&orderId={orderId}";

            var (success, responseBody) =
                await SendRequest("DELETE", "/api/v3/order", payload, true, true);

            if (!success)
                return null;

            var data = JObject.Parse(responseBody);

            return JsonConvert.DeserializeObject<CanceledOrderView>(data.ToString());
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var payload = $"symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/openOrders", payload, true, false);

            if (!success)
                return new List<OpenOrderView>();

            return JsonConvert.DeserializeObject<List<OpenOrderView>>(responseBody);
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/account", string.Empty, true, false);

            if (!success)
                return new List<AccBalance>();

            var data = JObject.Parse(responseBody)["balances"];

            return data == null
                ? new List<AccBalance>()
                : JsonConvert.DeserializeObject<List<AccBalance>>(data.ToString());
        }
        
        public async Task<List<string>> GetSelfSymbols()
        {
            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/selfSymbols", string.Empty, true, false);

            if (!success)
                return new List<string>();

            var data = JObject.Parse(responseBody)["data"];

            return data == null
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(data.ToString());
        }

        public async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var payload = $"symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v3/depth", payload, false, false);

            if (!success)
                return new OrderbookView();

            var data = JObject.Parse(responseBody);

            return JsonConvert.DeserializeObject<OrderbookView>(data.ToString());
        }

        private async Task<(bool, string)> SendRequest(string method, string endpoint, string payload = "",
            bool useSignature = false, bool logInfo = true, Uri otherUri = null)
        {
            var uri =
                otherUri != null
                    ? new Uri(otherUri, endpoint)
                    : new Uri(_baseUri, endpoint);

            if (logInfo)
                Log.Information($"MexcClient:SendRequest request {endpoint} {payload}");

            if (useSignature)
            {
                var timestamp = AppUtils.NowMilis();

                if (!string.IsNullOrEmpty(payload))
                {
                    payload = $"{payload}&timestamp={timestamp}";
                    var signature = HMAC_SHA256(payload, _secretKey);
                    payload = $"{payload}&signature={signature}";
                }
                else
                {
                    payload = $"timestamp={timestamp}";
                    var signature = HMAC_SHA256(payload, _secretKey);
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
                        response = await _httpClient.PostAsync($"{uri}?{payload}", null);
                        break;
                    case "PUT":
                        response = await _httpClient.PutAsync($"{uri}?{payload}", null);
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
                        Log.Information($"MexcClient:SendRequest response {endpoint} {payload} {responseBody}");
                    
                    return (true, responseBody);
                }

                responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<MexcError>(responseBody);

                Log.Error($"MexcClient:SendRequest response {endpoint} {payload} {responseBody}");

                var errorMessage = $"{error.Code} {error.Description}";

                return (false, errorMessage);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Log.Error("MexcClient request timeout {uri} {response}", uri, responseBody);
                else if (e is HttpRequestException)
                    Log.Error("MexcClient http error {uri} {response}", uri, responseBody);
                else
                    Log.Error(e, "MexcClient {response}", responseBody);

                return (false, string.Empty);
            }
        }

        private string HMAC_SHA256(string payload, string key)
        {
            if (string.IsNullOrEmpty(payload))
                return string.Empty;

            var hashMaker = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var data = Encoding.UTF8.GetBytes(payload);
            var hash = hashMaker.ComputeHash(data);

            var sb = new StringBuilder(hash.Length * 2);

            foreach (var b in hash)
                sb.Append($"{b:x2}");

            var hashString = Convert.ToString(sb);

            return hashString;
        }
    }
}