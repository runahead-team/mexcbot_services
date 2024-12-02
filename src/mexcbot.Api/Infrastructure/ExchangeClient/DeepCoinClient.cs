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
using mexcbot.Api.Models.DeepCoin;
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
    public class DeepCoinClient : ExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _passphrase;
        private readonly string _instType = "SPOT"; //Enum:"SPOT","SWAP"

        private readonly HttpClient _httpClient = new HttpClient();

        public DeepCoinClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public DeepCoinClient(string baseUrl, string apiKey, string secretKey, string passphrase)
        {
            _baseUri = new Uri(baseUrl);

            _apiKey = apiKey;
            _secretKey = secretKey;
            _passphrase = passphrase;
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var payload = $"instType={_instType}";
            payload = payload + $"&instId={@base}-{quote}";

            var (success, responseBody) =
                await SendRequest("GET", $"/deepcoin/market/instruments", payload, false, false);

            if (!success)
                return new ExchangeInfoView();

            var data = JToken.Parse(responseBody)["data"][0];

            if (data == null)
                return new ExchangeInfoView();

            var deepCoinExchangeInfo = JsonConvert.DeserializeObject<DeepCoinExchangeInfo>(data.ToString());

            return deepCoinExchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(deepCoinExchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            //Enum:"1m","5m","15m","30m","1H","4H","12H","1D","1W","1M","1Y"
            var payload = $"instId={@base}-{quote}";
            payload = payload + $"&bar={interval}&limit=300";

            var (success, responseBody) =
                await SendRequest("GET", "/deepcoin/market/candles", payload, false, false);

            if (!success)
                return [];
            
            var data = JObject.Parse(responseBody)["data"];

            return data == null ? [] : JsonConvert.DeserializeObject<List<JArray>>(data.ToString());
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var symbol = $"{@base}-{quote}";
            var payload = $"instType={_instType}";

            var (success, responseBody) =
                await SendRequest("GET", "/deepcoin/market/tickers", payload, false, false);

            if (!success)
                return new Ticker24hrView();

            var data = JObject.Parse(responseBody)["data"];

            if (data == null)
                return new Ticker24hrView();

            var tickers = JsonConvert.DeserializeObject<List<DeepCoinTicker24hr>>(data.ToString());
            var ticker = tickers.FirstOrDefault(x => x.Symbol == symbol);


            return ticker == null ? new Ticker24hrView() : new Ticker24hrView(ticker);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            //Enum:"isolated","cross","cash", cash for spot
            var tdMode = "cash";
            var sideStr = side.ToString().ToLower();
            var typeStr = OrderType.LIMIT.ToString().ToLower();
            var payload =
                $"instId={@base}-{quote}";
            payload = payload + $"&tdMode={tdMode}";
            payload = payload + $"&side={sideStr}";
            payload = payload + $"&ordType={typeStr}";
            payload = payload + $"&sz={quantity}";
            payload = payload + $"&px={price}";

            var (success, responseBody) =
                await SendRequest("POST", "/deepcoin/trade/order", payload, true, true);

            if (!success)
                return new OrderDto();

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new OrderDto();

            var deepCoinOrder = JsonConvert.DeserializeObject<DeepCoinOrder>(data.ToString());

            return deepCoinOrder == null ? new OrderDto() : new OrderDto(deepCoinOrder);
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            var payload = $"instId={@base}-{quote}&ordId={orderId}";

            var (success, responseBody) =
                await SendRequest("POST", "/deepcoin/trade/cancel-order", payload, true, true);

            if (!success)
                return null;

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new CanceledOrderView();

            var deepCoinCanceledOrder = JsonConvert.DeserializeObject<DeepCoinCanceledOrderView>(data.ToString());

            return deepCoinCanceledOrder == null
                ? new CanceledOrderView()
                : new CanceledOrderView(deepCoinCanceledOrder);
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var index = 0;
            var payload = $"instId={@base}-{quote}";

            var (success, responseBody) =
                await SendRequest("GET", "/deepcoin/trade/v2/orders-pending", payload, true, false);

            if (!success)
                return new List<OpenOrderView>();

            return JsonConvert.DeserializeObject<List<OpenOrderView>>(responseBody);
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var retry = 3;

            while (retry > 0)
            {
                //instType=SPOT,SWAP
                var (success, responseBody) =
                    await SendRequest("GET", "/deepcoin/account/balances", $"instType={_instType}", true, false);


                if (success)
                {
                    var data = JObject.Parse(responseBody)["data"];

                    if (data == null)
                        return [];

                    var balances = JsonConvert.DeserializeObject<List<DeepCoinAccBalance>>(data.ToString())
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
            var payload = $"instId={@base}-{quote}";
            payload = payload + "&sz=400";

            var (success, responseBody) =
                await SendRequest("GET", "/deepcoin/market/books", payload, false, false);

            if (!success)
                return new OrderbookView();

            var data = JObject.Parse(responseBody)["data"];
            if (data == null)
                return new OrderbookView();

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
                Log.Information($"DeepCoinClient:SendRequest request {endpoint} {payload}");

            if (useSignature)
            {
                var timestamp = AppUtils.NowDate().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                //HMAC_SHA256 with:"{timestamp} + {method} + {requestPath} + {body}"
                var preHashStr = $"{timestamp}{method}{endpoint}";
                preHashStr = preHashStr + $"?{payload}";

                var sign = GetHmacSha256Signature(preHashStr, _secretKey);
                
                _httpClient.DefaultRequestHeaders.Clear();

                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("DC-ACCESS-KEY", _apiKey);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("DC-ACCESS-SIGN", sign);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("DC-ACCESS-TIMESTAMP", timestamp);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("DC-ACCESS-PASSPHRASE", _passphrase);
            }

            string responseBody = null;

            try
            {
                HttpResponseMessage response;
                var requestUri = uri + $"?{payload}";

                switch (method)
                {
                    case "GET":
                        response = await _httpClient.GetAsync(requestUri);
                        break;
                    case "POST":
                        response = await _httpClient.PostAsync(requestUri, null);
                        break;
                    case "PUT":
                        response = await _httpClient.PutAsync(requestUri, null);
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync(requestUri);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    responseBody = await response.Content.ReadAsStringAsync();

                    if (logInfo)
                        Log.Information($"DeepCoinClient:SendRequest response {endpoint} {payload} {responseBody}");

                    return (true, responseBody);
                }

                responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<DeepCoinError>(responseBody);

                Log.Error($"DeepCoinClient:SendRequest response {endpoint} {payload} {responseBody}");

                var errorMessage = $"{error.Code} {error.Description}";

                return (false, errorMessage);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Log.Error("DeepCoinClient request timeout {uri} {response}", uri, responseBody);
                else if (e is HttpRequestException)
                    Log.Error("DeepCoinClient http error {uri} {response}", uri, responseBody);
                else
                    Log.Error(e, "DeepCoinClient {response}", responseBody);

                return (false, string.Empty);
            }
        }

        private static string GetHmacSha256Signature(string preHash, string secretKey)
        {
            // Convert the data and secret key to byte arrays
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(preHash);

            // Create the HMACSHA256 instance and compute the hash
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);

                // Convert the hash to a Base64 string
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}