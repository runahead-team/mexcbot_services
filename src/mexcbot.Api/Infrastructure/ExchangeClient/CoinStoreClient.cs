using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.CoinStore;
using mexcbot.Api.Models.LBank;
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
    public class CoinStoreClient : ExchangeClient
    {
        private Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _ordType = "LIMIT";

        private readonly HttpClient _httpClient = new HttpClient();

        public CoinStoreClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public CoinStoreClient(string baseUrl, string apiKey, string secretKey)
        {
            _baseUri = new Uri(baseUrl);

            _apiKey = apiKey;
            _secretKey = secretKey;
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var payload = new
            {
                symbolCodes = new List<string>()
                {
                    $"{@base}{quote}"
                }
            };

            var (success, responseBody) =
                await SendRequest("POST", $"/api/v2/public/config/spot/symbols", string.Empty, payload, false, false);

            if (!success)
                return new ExchangeInfoView();

            var data = JToken.Parse(responseBody)["data"][0];

            if (data == null)
                return new ExchangeInfoView();

            var coinStoreExchangeInfo = JsonConvert.DeserializeObject<CoinStoreExchangeInfo>(data.ToString());

            return coinStoreExchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(coinStoreExchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            //1min, 5min, 15min, 30min, 60min, 4hour, 12hour, 1day, 1week
            var symbol = $"{@base}{quote}";
            var @param = $"period={interval}";

            var (success, responseBody) =
                await SendRequest("GET", $"/api/v1/market/kline/{symbol}", @param, null, false, false);

            if (!success)
                return [];

            var data = JObject.Parse(responseBody)["data"]["item"];
            if (data == null)
                return [];

            var candleTicks = JsonConvert.DeserializeObject<List<CoinStoreCandleTick>>(data.ToString());
            var result = new List<JArray>();

            foreach (var candleTick in candleTicks)
            {
                result.Add([
                    candleTick.StartTime,
                    candleTick.Open,
                    candleTick.High,
                    candleTick.Low,
                    candleTick.Close,
                    candleTick.Volume,
                    candleTick.EndTime,
                    candleTick.Amount
                ]);
            }

            return result;
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var symbol = $"{@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v1/market/tickers", string.Empty, false, false);

            if (!success)
                return new Ticker24hrView();

            var data = JObject.Parse(responseBody)["data"];

            if (data == null)
                return new Ticker24hrView();

            var tickers = JsonConvert.DeserializeObject<List<CoinStoreTicker24hr>>(data.ToString());
            var ticker = tickers.FirstOrDefault(x => x.Symbol == symbol);

            return ticker == null ? new Ticker24hrView() : new Ticker24hrView(ticker);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var symbol = $"{@base}{quote}";
            var atQty = 0m;
            var atPrice = 0m;
            if (decimal.TryParse(quantity, new NumberFormatInfo(), out var parsedQty))
                atQty = parsedQty;
            if (decimal.TryParse(price, new NumberFormatInfo(), out var parsedPrice))
                atPrice = parsedPrice;

            var payload = new
            {
                symbol = symbol,
                side = side.ToString(),
                ordType = _ordType,
                ordPrice = atPrice,
                ordQty = atQty,
                timestamp = AppUtils.NowMilis()
            };

            var (success, responseBody) =
                await SendRequest("POST", "/api/trade/order/place", string.Empty, payload, true, true);

            if (!success)
                return new OrderDto();

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new OrderDto();

            var coinStoreOrder = JsonConvert.DeserializeObject<CoinStoreOrder>(data.ToString());
            coinStoreOrder.Symbol = symbol;
            coinStoreOrder.Price = symbol;
            coinStoreOrder.Side = side.ToString();
            coinStoreOrder.OrigQty = quantity;

            return new OrderDto(coinStoreOrder);
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            var symbol = $"{@base}{quote}";

            if (!long.TryParse(orderId, out var orderIdRequest) || orderIdRequest <= 0)
            {
                Log.Error($"CoinStoreClient:CancelOrder: {symbol} - {orderId}");
                return new CanceledOrderView();
            }

            var payload = new
            {
                symbol = symbol,
                ordId = orderIdRequest
            };

            var (success, responseBody) =
                await SendRequest("POST", "/api/trade/order/cancel", string.Empty, payload, true, true);

            if (!success)
                return null;

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new CanceledOrderView();

            var coinStoreCanceledOrder = JsonConvert.DeserializeObject<CoinStoreCanceledOrderView>(data.ToString());
            coinStoreCanceledOrder.Symbol = symbol;

            return new CanceledOrderView(coinStoreCanceledOrder);
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var @params = $"symbol={@base}{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/api/v2/trade/order/active", @params, null, true, false);

            if (!success)
                return new List<OpenOrderView>();

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new List<OpenOrderView>();

            var coinStoreOpenOrders = JsonConvert.DeserializeObject<List<CoinStoreOpenOrderView>>(data.ToString());

            var result = coinStoreOpenOrders.Select(x => new OpenOrderView(x)).ToList();

            return result.Count > 0 ? result : [];
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var retry = 3;

            while (retry > 0)
            {
                //instType=SPOT,SWAP
                var (success, responseBody) =
                    await SendRequest("POST", "/api/spot/accountList", string.Empty, null, true, false);


                if (success)
                {
                    var data = JObject.Parse(responseBody)["data"];

                    if (data == null)
                        return [];

                    var balances = JsonConvert.DeserializeObject<List<CoinStoreAccBalance>>(data.ToString())
                        .Where(x=>x.Type == 1)
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
            var @params = $"depth={100}";

            var (success, responseBody) =
                await SendRequest("GET", $"api/v1/market/depth/{symbol}", @params, null, false, false);

            if (!success)
                return new OrderbookView();

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new OrderbookView();

            var coinStoreOrderbook = JsonConvert.DeserializeObject<CoinStoreOrderbookView>(data.ToString());

            return new OrderbookView(coinStoreOrderbook);
        }

        private async Task<(bool, string)> SendRequest(string method, string endpoint, string @params = "",
            object payload = null, bool useSignature = false, bool logInfo = true, Uri otherUri = null)
        {
            var uri =
                otherUri != null
                    ? new Uri(otherUri, endpoint)
                    : new Uri(_baseUri, endpoint);

            if (logInfo)
                Log.Information($"CoinStoreClient:SendRequest request {endpoint} {@params}");

            payload ??= new { };
            var payloadStr = JsonConvert.SerializeObject(payload);

            string responseBody = null;

            try
            {
                var requestUri = uri;
                if (!string.IsNullOrEmpty(@params))
                    requestUri = new Uri(_baseUri, $"{endpoint}?{@params}");

                HttpMethod httpMethod;
                
                switch (method)
                {
                    case "GET":
                        httpMethod = HttpMethod.Get;
                        break;
                    case "POST":
                        httpMethod = HttpMethod.Post;
                        break;
                    case "PUT":
                        httpMethod = HttpMethod.Put;
                        break;
                    case "DELETE":
                        httpMethod = HttpMethod.Delete;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                var requestMessage = new HttpRequestMessage(httpMethod, requestUri)
                {
                    Content = new StringContent(payloadStr, Encoding.UTF8, "application/json")
                };
                
                if (useSignature)
                {
                    var timestamp = AppUtils.NowMilis();
                    var timestampStr = timestamp.ToString();
                    var expiresKey = (Math.Floor(timestamp / 30000.0)).ToString("0");
                    var expiresKeyBytes = Encoding.UTF8.GetBytes(expiresKey);

                    // Step 1: Generate the HMAC key
                    var key = GenerateHmacSha256(_secretKey, expiresKeyBytes);

                    var preHashStr = "";

                    if (!string.IsNullOrEmpty(@params))
                        preHashStr = $"{@params}";

                    if (!string.IsNullOrEmpty(payloadStr))
                        preHashStr = preHashStr + $"{payloadStr}";

                    var payloadBytes = Encoding.UTF8.GetBytes(preHashStr);
                    
                    var sign = GenerateHmacSha256(key,payloadBytes);

                    requestMessage.Headers.Add("X-CS-APIKEY", _apiKey);
                    requestMessage.Headers.Add("X-CS-SIGN", sign);
                    requestMessage.Headers.Add("X-CS-EXPIRES", timestampStr);
                }

                var response = await _httpClient.SendAsync(requestMessage);
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    responseBody = await response.Content.ReadAsStringAsync();

                    if (logInfo)
                        Log.Information($"CoinStoreClient:SendRequest response {endpoint} {@params} {responseBody}");

                    return (true, responseBody);
                }

                responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<CoinStoreError>(responseBody);

                Log.Error($"CoinStoreClient:SendRequest response {endpoint} {@params} {responseBody}");

                var errorMessage = $"{error.Code} {error.Description}";

                return (false, errorMessage);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Log.Error("CoinStoreClient request timeout {uri} {response}", uri, responseBody);
                else if (e is HttpRequestException)
                    Log.Error("CoinStoreClient http error {uri} {response}", uri, responseBody);
                else
                    Log.Error(e, "CoinStoreClient {response}", responseBody);

                return (false, string.Empty);
            }
        }

        private static string GenerateHmacSha256(string key, byte[] data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();  // Convert to hex string
        }
    }
}