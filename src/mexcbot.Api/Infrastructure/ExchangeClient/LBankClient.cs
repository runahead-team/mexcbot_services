using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.LBank;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Serilog;
using Newtonsoft.Json.Linq;
using sp.Core.Utils;

namespace mexcbot.Api.Infrastructure.ExchangeClient
{
    public class LBankClient : ExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;

        private readonly HttpClient _httpClient = new HttpClient();

        public LBankClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public LBankClient(string baseUrl, string apiKey, string secretKey)
        {
            _baseUri = new Uri(baseUrl);

            _apiKey = apiKey;
            _secretKey = secretKey;
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var payload = $"symbol={@base.ToLower()}_{quote.ToLower()}";

            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Get, "/v2/accuracy.do", false, payload, false);

            if (!success)
                return new ExchangeInfoView();

            var lBankExchangeInfo = JsonConvert.DeserializeObject<LBankExchangeInfo>(responseBody.First().ToString());

            return lBankExchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(lBankExchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            //type: minute1, minute5, minute15, minute30, hour1, hour4, hour8, hour12, day1, week1, month1
            var payload = $"symbol={@base.ToLower()}_{quote.ToLower()}&type={interval}";

            var (success, responseBody) =
                await SendRequest<string>(HttpMethod.Get, "/v2/kline.do", false, payload, false);

            if (!success)
                return new List<JArray>();

            return JsonConvert.DeserializeObject<List<JArray>>(responseBody);
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";
            var payload = $"symbol={baseSymbol}";

            var (success, responseBody) =
                await SendRequest<string>(HttpMethod.Get, "/v2/ticker/24hr.do", false, payload, false);

            if (!success)
                return new Ticker24hrView();

            var data = JObject.Parse(responseBody)["ticker"];

            if (data == null)
                return new Ticker24hrView();

            var lBankTicker24Hr = JsonConvert.DeserializeObject<LBankTicker24hr>(data.ToString());

            return lBankTicker24Hr == null ? new Ticker24hrView() : new Ticker24hrView(baseSymbol, lBankTicker24Hr);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";
            var type = side == OrderSide.BUY ? "buy" : "sell";

            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, $"v2/create_order.do", true,
                    new
                    {
                        symbol = baseSymbol,
                        type = type,
                        price = price,
                        amount = quantity
                    });

            if (!success)
                return new OrderDto();

            return new OrderDto
            {
                OrderListId = 0,
                OrderId = (string)response.order_id,
                Symbol = baseSymbol,
                Price = price,
                Side = type,
                OrigQty = quantity,
                Status = OrderStatus.NEW
            };
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "v2/cancel_order.do", true, new
                {
                    symbol = symbol,
                    order_id = orderId
                });

            return JsonConvert.DeserializeObject<CanceledOrderView>(response.ToString());
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
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
                return new List<OpenOrderView>();

            var orders = response["orders"]?.ToObject<List<dynamic>>();

            if (orders == null)
                return new List<OpenOrderView>();

            if (!orders.Any())
                return new List<OpenOrderView>();

            return orders.Select(item => new OpenOrderView
            {
                OrderId = (string)item.order_id,
                Symbol = (string)item.symbol,
                Price = (string)item.price,
            }).ToList();
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Post, "/v2/user_info.do", true, null, true);

            if (!success)
                return new List<AccBalance>();

            var data = responseBody["balances"];

            return data == null
                ? new List<AccBalance>()
                : JsonConvert.DeserializeObject<List<AccBalance>>(data.ToString());
        }

        public async Task<List<string>> GetSelfSymbols()
        {
            var (success, responseBody) =
                await SendRequest<string>(HttpMethod.Get, "/v2/currencyPairs.do", false, string.Empty, true);

            if (!success)
                return new List<string>();

            var data = JObject.Parse(responseBody)["data"];

            return data == null
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(data.ToString());
        }

        public async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";
            var payload = $"symbol={baseSymbol}";

            var (success, responseBody) =
                await SendRequest<string>(HttpMethod.Get, "/v2/depth.do", false, payload, false);

            if (!success)
                return new OrderbookView();

            var data = JObject.Parse(responseBody);

            return JsonConvert.DeserializeObject<OrderbookView>(data.ToString());
        }

        private async Task<long> GetTimestamp()
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"/v2/timestamp.do");

            if (!success || response is null)
                return 0;

            return (long)response;
        }

        private async Task<(bool, T)> SendRequest<T>(HttpMethod method, string endpoint, bool isAuth = false,
            object body = null, bool ignored400 = false)
        {
            try
            {
                var requestBody = new List<KeyValuePair<string, string>>();
                var uri = new Uri(_baseUri, endpoint);
                var requestMessage = new HttpRequestMessage(method, new Uri(_baseUri, endpoint));
                var parameters = "";

                if (isAuth)
                {
                    var timestamp = await GetTimestamp();
                    var echoStr = AppUtils.NewGuidStr();

                    var bodyJson = new JObject();

                    if (body != null)
                    {
                        bodyJson = JObject.FromObject(body);
                    }

                    bodyJson.Add("api_key", _apiKey);
                    bodyJson.Add("echostr", echoStr);
                    bodyJson.Add("signature_method", "HmacSHA256");
                    bodyJson.Add("timestamp", timestamp.ToString());

                    var sortProperties = bodyJson.Properties()
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