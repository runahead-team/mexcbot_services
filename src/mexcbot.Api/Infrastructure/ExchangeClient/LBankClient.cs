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
using mexcbot.Api.Models.LBank;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Serilog;
using Newtonsoft.Json.Linq;
using sp.Core.Exceptions;
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
            var endpoint = $"symbol={@base.ToLower()}_{quote.ToLower()}";

            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Get, $"/v2/accuracy.do?{endpoint}", false, null, false);

            if (!success)
                return new ExchangeInfoView();

            var dataStr = responseBody.First().ToString();

            var lBankExchangeInfo = JsonConvert.DeserializeObject<LBankExchangeInfo>(dataStr);

            return lBankExchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(lBankExchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            var startDate = AppUtils.NowMilis() - (AppUtils.NowMilis() % 86400000);
            var queryFromTime = startDate - 86400000;

            //type: minute1, minute5, minute15, minute30, hour1, hour4, hour8, hour12, day1, week1, month1
            var payload = $"symbol={@base.ToLower()}_{quote.ToLower()}";
            payload += $"&size=2000&type={interval}";
            payload += $"&time={(queryFromTime / 1000).ToString()}";

            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Get, $"/v2/kline.do?{payload}", false, null, false);

            var result = new List<JArray>();

            if (!success)
                return result;

            var dataStr = responseBody.ToString();

            result = JsonConvert.DeserializeObject<List<JArray>>(dataStr);

            result = result?.Select(x =>
            {
                if (!long.TryParse(x.First().ToString(), out var timeSeconds))
                {
                    Log.Error("Candle stick parse time fail");
                    throw new AppException();
                }

                var jToken = JToken.FromObject(x);
                jToken[0] = timeSeconds * 1000;
                var jTokenStr = jToken.ToString();
                var jArr = JArray.Parse(jTokenStr);

                return jArr;
            }).ToList();

            return result;
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";
            var payload = $"symbol={baseSymbol}";

            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Get, $"/v2/ticker/24hr.do?{payload}", false, null, false);

            if (!success)
                return new Ticker24hrView();

            var data = JObject.Parse(responseBody[0].ToString())["ticker"];

            if (data == null)
                return new Ticker24hrView();

            var dataStr = data.ToString();

            var lBankTicker24Hr = JsonConvert.DeserializeObject<LBankTicker24hr>(dataStr);

            return lBankTicker24Hr == null ? new Ticker24hrView() : new Ticker24hrView(baseSymbol, lBankTicker24Hr);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";
            var type = side == OrderSide.BUY ? "buy" : "sell";

            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, $"/v2/supplement/create_order.do", true,
                    new
                    {
                        symbol = baseSymbol,
                        type = type,
                        price = price,
                        amount = quantity
                    }, false, true);

            if (!success)
                return new OrderDto();

            return new OrderDto
            {
                OrderListId = 0,
                BotExchangeType = BotExchangeType.LBANK,
                OrderId = (string)response.order_id,
                Symbol = baseSymbol,
                Price = price,
                Type = "LIMIT",
                Side = type,
                OrigQty = quantity,
                TransactTime = AppUtils.NowMilis(),
                Status = OrderStatus.NEW
            };
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            Log.Information("Bot cancel order {0} {1} {2}", @base, quote, orderId);

            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "/v2/supplement/cancel_order.do", true, new
                {
                    symbol = symbol,
                    orderId = orderId
                });

            if (!success || response is null)
                return new CanceledOrderView();

            var data = response.ToObject<dynamic>();

            if (data is null)
                return new CanceledOrderView();

            return new CanceledOrderView
            {
                OrderId = (string)data.orderId,
                Symbol = (string)data.symbol,
                OrigClientOrderId = (string)data.origClientOrderId,
                Price = (string)data.price,
                OrigQty = (string)data.origQty,
                ExecutedQty = (string)data.executedQty,
                Side = (string)data.tradeType,
                TimeInForce = (string)data.timeInForce,
                LbankOrderStatus = (string)data.status
            };
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";
            symbol = symbol.ToLower();

            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "/v2/supplement/orders_info_no_deal.do", true, new
                {
                    symbol = symbol,
                    current_page = "1",
                    page_length = "100"
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
                OrderId = (string)item.orderId,
                Symbol = (string)item.symbol,
                Price = (string)item.price,
                Side = (string)item.type,
                OrigQty = (string)item.origQty
            }).ToList();
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var retry = 3;

            while (retry > 0)
            {
                var (success, responseBody) =
                    await SendRequest<JObject>(HttpMethod.Post, "/v2/supplement/user_info_account.do", true);

                if (!success)
                    return new List<AccBalance>();

                var data = responseBody["balances"];

                if (data == null)
                    return new List<AccBalance>();

                var balances = JsonConvert.DeserializeObject<List<AccBalance>>(data.ToString())
                    .Where(x => decimal.Parse(x.Free, new NumberFormatInfo()) > 0m).Select(x => new AccBalance()
                    {
                        Asset = x.Asset,
                        Free = x.Free
                    }).ToList();

                if (balances.Count > 0)
                    return balances;

                retry--;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            return new List<AccBalance>();
        }

        public async Task<List<string>> GetSelfSymbols()
        {
            var (success, responseBody) =
                await SendRequest<JArray>(HttpMethod.Get, "/v2/currencyPairs.do", false, string.Empty, true);

            if (!success)
                return new List<string>();

            return responseBody == null
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(responseBody.ToString());
        }

        public async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var baseSymbol = $"{@base.ToLower()}_{quote.ToLower()}";

            //default size
            var payload = $"symbol={baseSymbol}&size=100";

            var (success, responseBody) =
                await SendRequest<JObject>(HttpMethod.Get, $"/v2/depth.do?{payload}", false, null, false);

            if (!success)
                return new OrderbookView();

            return JsonConvert.DeserializeObject<OrderbookView>(responseBody.ToString());
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
            object body = null, bool ignored400 = false, bool logResponse = false)
        {
            try
            {
                var requestBody = new List<KeyValuePair<string, string>>();
                var requestMessage = new HttpRequestMessage(method, new Uri(_baseUri, endpoint));
                var parameters = "";

                if (isAuth)
                {
                    var timestamp = await GetTimestamp();
                    var echoStr = AppUtils.RandomString(35);

                    var bodyJson = new JObject();

                    if (body != null)
                    {
                        bodyJson = JObject.FromObject(body);
                    }

                    bodyJson.Add("timestamp", timestamp.ToString());
                    bodyJson.Add("signature_method", "HmacSHA256");
                    bodyJson.Add("echostr", echoStr);
                    bodyJson.Add("api_key", _apiKey);
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

                        requestBody.Add(
                            new KeyValuePair<string, string>(key, value));
                    }

                    var repairedStr = HMAC_MD5(parameters).ToUpper();
                    var signature = HMAC_SHA256(repairedStr, _secretKey);

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

                if (logResponse)
                    Log.Information("Lbank response {0}", responseBody);

                var responseObj = JsonConvert.DeserializeObject<JObject>(responseBody);

                var errorCode = int.Parse(responseObj["error_code"].ToString());

                if (errorCode > 0 && errorCode != 10025)
                {
                    Log.Error("{client} {endpoint} {request} {response}", GetType().Name, endpoint, body, responseBody);
                }

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