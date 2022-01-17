using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using DefaultNamespace;
using multexbot.Api.Constants;
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Utils;

namespace multexbot.Api.Infrastructure.ExchangeClient
{
    public class LBankExchangeClient : BaseExchangeClient
    {
        private readonly string _apiKey;
        private readonly Uri _baseUri;
        private readonly string _secretKey;

        private readonly HttpClient _httpClient = new HttpClient();

        public LBankExchangeClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public LBankExchangeClient(string baseUrl, string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;
            _baseUri = new Uri(baseUrl);
        }

        public override async Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(
            string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"v2/ticker.do?symbol={symbol}");

            if (!success || response is null)
                return (0, 0, 0);

            var data = ((JArray)response).FirstOrDefault();

            if (data == null)
                return (0, 0, 0);

            var ticker = data["ticker"]?.ToObject<dynamic>();

            return ticker == null
                ? (0, 0, 0)
                : ((decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice))(ticker.latest, ticker.latest, 0);
        }

        public override async Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var type = side == OrderSide.BUY ? "buy" : "sell";

            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, $"v2/create_order.do", true,
                new
                {
                    symbol = symbol,
                    type = type,
                    price = price,
                    amount = amount
                });

            if (!success)
                return null;

            return new OrderDto
            {
                ExternalId = 0,
                ExternalUuid = (string)response.order_id,
                Symbol = symbol,
                Base = @base,
                Quote = quote,
                Price = price,
                Side = side,
                Qty = amount,
                Filled = 0,
                Total = amount * price,
                Status = OrderStatus.OPEN
            };
        }

        public override async Task<List<OrderDto>> GetOpenOrders(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "v2/orders_info_history.do", true, new
                {
                    api_key = _apiKey,
                    symbol = symbol,
                    status = 0,
                });

            if (!success || response is null)
                return new List<OrderDto>();

            var orders = response["orders"]?.ToObject<List<dynamic>>();

            if (orders == null)
                return new List<OrderDto>();

            if (!orders.Any())
                return new List<OrderDto>();

            return orders.Select(item => new OrderDto
            {
                ExternalId = (long)item.ordNo,
                ExternalUuid = (string)item.order_id,
                Symbol = (string)item.symbol,
                Price = (decimal)item.price,
                Qty = (decimal)item.amount,
                Filled = (decimal)item.deal_amount,
                // Total = (decimal) item.record,
                // Status = ConvertOrderStatus((string) item.state)
            }).ToList();
        }

        public override async Task<bool> Cancel(string id, string @base = null, string quote = null)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "v2/cancel_order.do", true, new
                {
                    api_key = _apiKey,
                    symbol = symbol,
                    order_id = id
                });

            return success;
        }

        public override async Task<Dictionary<string, decimal>> GetFunds(params string[] coins)
        {
            coins = coins.Select(x => x.ToLower()).ToArray();

            var result = new Dictionary<string, decimal>();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, $"v2/user_info.do", true);

            if (!success || response is null)
                return result;

            var funds = response["free"]?.ToObject<Dictionary<string, decimal>>();

            if (funds == null)
                return result;

            if (!funds.Any())
                return result;

            foreach (var coin in coins)
            {
                result.Add(coin.ToUpper(), funds.GetValueOrDefault(coin));
            }

            return result;
        }

        public override async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Get, $"v2/depth.do?symbol={symbol}&size=100");

            if (!success || response is null)
                return new OrderbookView();

            var orderbook = new OrderbookView();

            var bids = response["bids"].ToObject<List<decimal[]>>();

            bids.ForEach(x =>
            {
                orderbook.Bids.Add(x);
            });

            var asks = response["asks"].ToObject<List<decimal[]>>();

            asks.ForEach(x =>
            {
                orderbook.Asks.Add(x);
            });

            return orderbook;
        }

        private async Task<long> GetTimestamp()
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"v2/timestamp.do");

            if (!success || response is null)
                return 0;

            return (long)response;
        }

        private async Task<(bool, T)> SendRequest<T>(HttpMethod method, string endpoint, bool isAuthentication = false,
            object body = null, bool ignored400 = false)
        {
            try
            {
                var requestBody = new List<KeyValuePair<string, string>>();

                var requestMessage = new HttpRequestMessage(method, new Uri(_baseUri, endpoint));

                if (isAuthentication)
                {
                    var timestamp = await GetTimestamp();
                    var echoStr = AppUtils.NewGuidStr();

                    string parameters = "";

                    var bodyJson = new JObject();

                    if (body != null)
                    {
                        bodyJson = JObject.FromObject(body);
                    }

                    bodyJson.Add("api_key", _apiKey);
                    bodyJson.Add("echostr", echoStr);
                    bodyJson.Add("signature_method", "HmacSHA256");
                    bodyJson.Add("timestamp", timestamp.ToString());

                    var sortProperties= bodyJson.Properties()
                        .OrderBy(x => x.Name)
                        .ToList();

                    foreach (var property in sortProperties)
                    {
                        var key = property.Name;
                        var value = property.Value.ToString();

                        if (sortProperties.Last() == property)
                            parameters += $"{key}={value}";
                        else
                            parameters += $"{key}={value}&";

                        requestBody.Add(new KeyValuePair<string, string>(key, value));
                    }

                    var signature = HMAC_SHA256(HMAC_MD5(parameters), _secretKey);
                    requestBody.Add(new KeyValuePair<string, string>("sign", signature));

                    var requestContent = new FormUrlEncodedContent(requestBody);

                    requestMessage.Content = requestContent;
                }

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (ignored400)
                        return (false, default);

                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Log.Warning("{client} {endpoint} {request} {responseCode}", GetType().Name, endpoint,
                        errorResponse, response.StatusCode);

                    return (false, default);
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                Log.Information("{client} {endpoint} {request} {responseCode}", GetType().Name, endpoint,
                    responseBody, response.StatusCode);

                var responseObj = JsonConvert.DeserializeObject<JObject>(responseBody);

                var success = responseObj["result"];

                var data = responseObj["data"]?.ToObject<dynamic>();

                return success == null ? (false, default) : ((bool, T))(success, data);
            }
            catch (Exception e)
            {
                Log.Error(e, "{client} {endpoint} {request}", GetType().Name, endpoint, body);

                return (false, default);
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

        private string HMAC_MD5(string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return string.Empty;

            var hash = MD5.Create().ComputeHash(new UTF8Encoding().GetBytes(parameters));

            var sb = new StringBuilder(hash.Length * 2);

            foreach (var b in hash)
                sb.Append($"{b:X2}");

            var hashString = Convert.ToString(sb);

            return hashString;
        }
    }
}