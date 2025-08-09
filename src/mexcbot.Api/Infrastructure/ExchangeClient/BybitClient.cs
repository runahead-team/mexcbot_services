using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Bybit;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Serilog;
using Newtonsoft.Json.Linq;
using sp.Core.Extensions;
using sp.Core.Utils;

namespace mexcbot.Api.Infrastructure.ExchangeClient
{
    public class BybitClient : ExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _category = "spot"; // Category for spot trading

        private readonly HttpClient _httpClient = new HttpClient();

        public BybitClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public BybitClient(string baseUrl, string apiKey, string secretKey)
        {
            _baseUri = new Uri(baseUrl);

            _apiKey = apiKey;
            _secretKey = secretKey;
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var payload = $"category={_category}";
            payload = payload + $"&symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", $"/v5/market/instruments-info", payload);

            if (!success)
                return new ExchangeInfoView();

            var response = JObject.Parse(responseBody);
            var data = response["result"]?["list"]?[0];

            if (data == null)
                return new ExchangeInfoView();

            var bybitExchangeInfo = JsonConvert.DeserializeObject<BybitExchangeInfo>(data.ToString());

            return bybitExchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(bybitExchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            // Bybit intervals: 1,3,5,15,30,60,120,240,360,720,D,M,W
            var symbol = $"{@base}{quote}";
            var payload = $"category={_category}&symbol={symbol}&interval={interval}&limit=2000";

            var (success, responseBody) =
                await SendRequest("GET", "/v5/market/kline", payload);

            if (!success)
                return [];

            var response = JObject.Parse(responseBody);
            var data = response["result"]?["list"];

            var candleTicks = data == null ? [] : JsonConvert.DeserializeObject<List<JArray>>(data.ToString());

            return candleTicks;
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var symbol = $"{@base}{quote}";
            var payload = $"category={_category}&symbol={symbol}";

            var (success, responseBody) =
                await SendRequest("GET", "/v5/market/tickers", payload);

            if (!success)
                return new Ticker24hrView();

            var response = JObject.Parse(responseBody);
            var data = response["result"]?["list"]?[0];

            if (data == null)
                return new Ticker24hrView();

            var ticker = JsonConvert.DeserializeObject<BybitTicker24hr>(data.ToString());

            return ticker == null ? new Ticker24hrView() : new Ticker24hrView(ticker);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var symbol = $"{@base}{quote}";
            var sideStr = side.ToString();
            var orderType = "Limit";

            var payload = new
            {
                category = _category,
                symbol = symbol,
                side = sideStr,
                orderType = orderType,
                qty = quantity,
                price = price,
                timeInForce = "GTC"
            };

            var (success, responseBody) =
                await SendRequest("POST", "/v5/order/create", string.Empty, payload, true, true);

            if (!success)
                return new OrderDto();

            var response = JObject.Parse(responseBody);
            var data = response["result"];
            if (data == null)
                return new OrderDto();

            var bybitOrder = new BybitOrder
            {
                OrderId = data["orderId"]?.ToString(),
                Symbol = symbol,
                Side = sideStr,
                OrigQty = quantity,
                Price = price,
                CreateTime = AppUtils.NowMilis().ToString()
            };

            return new OrderDto(bybitOrder);
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            var symbol = $"{@base}{quote}";

            var payload = new
            {
                category = _category,
                symbol = symbol,
                orderId = orderId
            };

            var (success, responseBody) =
                await SendRequest("POST", "/v5/order/cancel", string.Empty, payload, true, true);

            if (!success)
                return null;

            var response = JObject.Parse(responseBody);
            var data = response["result"];

            if (data == null)
                return new CanceledOrderView();

            // For Bybit, we return a simple canceled order view
            return new CanceledOrderView
            {
                Symbol = symbol,
                OrderId = orderId,
                Status = OrderStatus.CANCELED
            };
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var symbol = $"{@base}{quote}";
            var payload = $"category={_category}&symbol={symbol}&openOnly=0";

            var (success, responseBody) =
                await SendRequest("GET", "/v5/order/realtime", payload, true);

            if (!success)
                return new List<OpenOrderView>();

            var response = JObject.Parse(responseBody);
            var data = response["result"]?["list"];

            if (data == null)
                return new List<OpenOrderView>();

            var orders = JsonConvert.DeserializeObject<List<BybitOrder>>(data.ToString());
            var result = orders.Where(x => x.Status == "New" || x.Status == "PartiallyFilled")
                .Select(x => new OpenOrderView
                {
                    Symbol = x.Symbol,
                    OrderId = x.OrderId,
                    Price = x.Price,
                    OrigQty = x.OrigQty,
                    ExecutedQty = x.ExecutedQty,
                    Side = x.Side,
                    Status = x.Status,
                    Time = long.TryParse(x.CreateTime, out var time) ? time : 0
                }).ToList();

            return result;
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var retry = 3;

            while (retry > 0)
            {
                var payload = $"accountType=UNIFIED";

                var (success, responseBody) =
                    await SendRequest("GET", "/v5/account/wallet-balance", payload, true, true);

                if (success)
                {
                    var response = JObject.Parse(responseBody);
                    var data = response["result"]?["list"]?[0]?["coin"];

                    if (data == null)
                        return [];

                    var balances = JsonConvert.DeserializeObject<List<BybitAccBalance>>(data.ToString())
                        .Where(x => decimal.Parse(x.Free, new NumberFormatInfo()) > 0m)
                        .Select(x => new AccBalance()
                        {
                            Asset = x.Asset,
                            Free = x.Free
                        }).ToList();

                    return balances;
                }

                retry--;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            return new List<AccBalance>();
        }

        public Task<List<string>> GetSelfSymbols()
        {
            return Task.FromResult<List<string>>([]);
        }

        public async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var symbol = $"{@base}{quote}";
            var payload = $"category={_category}&symbol={symbol}&limit=50";

            var (success, responseBody) =
                await SendRequest("GET", "/v5/market/orderbook", payload, false, false);

            if (!success)
                return new OrderbookView();

            var response = JObject.Parse(responseBody);
            var data = response["result"];
            if (data == null)
                return new OrderbookView();

            var bybitOrderbook = JsonConvert.DeserializeObject<BybitOrderbook>(data.ToString());

            return new OrderbookView(bybitOrderbook);
        }

        private async Task<(bool, string)> SendRequest(string method, string endpoint, string payload = "",
            object jsonPayload = null, bool useSignature = false, bool logRequest = false, bool logResponse = false,
            Uri otherUri = null)
        {
            var uri =
                otherUri != null
                    ? new Uri(otherUri, endpoint)
                    : new Uri(_baseUri, endpoint);

            if (logRequest)
                Log.Information($"BybitClient {method} {endpoint} {payload}");

            string responseBody = null;

            try
            {
                HttpRequestMessage requestMessage;
                var requestUri = uri;

                if (method == "GET" && !string.IsNullOrEmpty(payload))
                {
                    requestUri = new Uri(_baseUri, $"{endpoint}?{payload}");
                }

                requestMessage = new HttpRequestMessage(method switch
                {
                    "GET" => HttpMethod.Get,
                    "POST" => HttpMethod.Post,
                    "PUT" => HttpMethod.Put,
                    "DELETE" => HttpMethod.Delete,
                    _ => throw new ArgumentOutOfRangeException()
                }, requestUri);

                if (method == "POST" && jsonPayload != null)
                {
                    var jsonContent = JsonConvert.SerializeObject(jsonPayload);
                    requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }

                if (useSignature)
                {
                    var timestamp = AppUtils.NowMilis().ToString();
                    var recvWindow = "5000";

                    // Build the signature string according to Bybit V5 API docs
                    var paramStr = "";
                    if (method == "GET" && !string.IsNullOrEmpty(payload))
                    {
                        paramStr = payload;
                    }
                    else if (method == "POST" && jsonPayload != null)
                    {
                        paramStr = JsonConvert.SerializeObject(jsonPayload);
                    }

                    var signaturePayload = timestamp + _apiKey + recvWindow + paramStr;
                    var signature = GetHmacSha256Signature(signaturePayload, _secretKey);

                    requestMessage.Headers.Add("X-BAPI-API-KEY", _apiKey);
                    requestMessage.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
                    requestMessage.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);
                    requestMessage.Headers.Add("X-BAPI-SIGN", signature);
                }

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    responseBody = await response.Content.ReadAsStringAsync();

                    if (logResponse)
                        Log.Information($"BybitClient {method} {endpoint} {payload} {responseBody}");

                    return (true, responseBody);
                }

                responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<BybitError>(responseBody);

                Log.Error($"BybitClient {method} {endpoint} {payload} {responseBody}");

                var errorMessage = $"{error.Code} {error.Description}";

                return (false, errorMessage);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Log.Error("BybitClient request timeout {uri} {response}", uri, responseBody);
                else if (e is HttpRequestException)
                    Log.Error("BybitClient http error {uri} {response}", uri, responseBody);
                else
                    Log.Error(e, "BybitClient {response}", responseBody);

                return (false, string.Empty);
            }
        }

        private static string GetHmacSha256Signature(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}