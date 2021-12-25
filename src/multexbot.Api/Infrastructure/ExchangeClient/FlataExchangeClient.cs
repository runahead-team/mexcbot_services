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
using Serilog;
using Newtonsoft.Json.Linq;

namespace multexbot.Api.Infrastructure.ExchangeClient
{
    public class FlataExchangeClient : BaseExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private string _sessionId = string.Empty;

        private readonly HttpClient _httpClient = new HttpClient();

        public FlataExchangeClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public FlataExchangeClient(string baseUrl, string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _baseUri = new Uri(baseUrl);
            _secretKey = secretKey;
            _sessionId = GetSessionId().Result;
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Session", _sessionId);
        }

        public override async Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(
            string @base, string quote)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"out/api/getSnapshot?symbol={@base}/{quote}");

            if (!success || response is null)
                return (0, 0, 0);

            var ticker = response.ticker;

            return (ticker.current, ticker.current, 0);
        }

        public override async Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side)
        {
            Log.Error(
                $"[Console] qty={amount} & price={price} & side={side}");
            
            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Post, "out/api/trading/newOrder", new
                {
                    symbol = $"{@base}/{quote}",
                    ordQty = amount,
                    ordPrc = price,
                    buySellType = side == OrderSide.BUY ? 1 : 2,
                    ordPrcType = 2, //"market price(2)",
                    ordFee = 0
                });

            if (!success)
                return null;

            var data = response["item"].ToObject<dynamic>();

            return new OrderDto
            {
                ExternalId = (long)data.ordNo,
                Symbol = $"{@base}/{quote}",
                Base = @base,
                Quote = quote,
                Price = price,
                Side = side,
                Qty = amount
            };
        }

        public override async Task<List<OrderDto>> GetOpenOrders(string @base, string quote)
        {
            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Get,
                    $"out/api/trading/getOrderList?trgtCurCd={quote}&setlCurCd={@base}&contflag=0&contKey=&reqcount=100");

            if (!success || response is null)
                return new List<OrderDto>();

            var records = response["record"].ToObject<List<dynamic>>();

            if(!records.Any())
                return new List<OrderDto>();

            return records.Select(item => new OrderDto
            {
                Id = (long)item.ordNo,
                Symbol = (string)item.symbol,
                Price = (decimal)item.ordPrc,
                Qty = (decimal)item.remnQty,
                // Filled = (decimal) item.record,
                // Total = (decimal) item.record,
                // Status = ConvertOrderStatus((string) item.state)
            }).ToList();
        }

        public override async Task<bool> Cancel(string id, string uuid, string @base = null, string quote = null)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, "out/api/trading/cancelOrder", new
                {
                    orgOrdNo = long.Parse(id)
                });

            return success;
        }

        public override async Task<Dictionary<string, decimal>> GetFunds(params string[] coins)
        {
            var result = new Dictionary<string, decimal>();

            foreach (string coin in coins)
            {
                var (success, response) =
                    await SendRequest<JObject>(HttpMethod.Get, $"out/api/trading/getCurrBalanceInfo?contflag=0&contKey={coin}&reqcount=100 ");

                if (!success || response is null)
                    return new Dictionary<string, decimal>();

                var data = response["record"];

                if (data.Count() == 0)
                    continue;

                var funds = data.ToObject<List<dynamic>>();

                foreach(var fund in funds)
                {
                    result.Add((string)fund.curCd, (decimal)fund.boldAmt);
                }
            }

            return result;
        }

        public override async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var (success, response) =
                await SendRequest<JObject>(HttpMethod.Get, $"out/api/getSnapshot?symbol={@base}/{quote}");

            if (!success || response is null)
                return new OrderbookView();

            var orderbook = new OrderbookView();

            var bids = response["bid"].ToObject<List<dynamic>>();

            bids.ForEach(x =>
            {
                orderbook.Bids.Add(new decimal[]
                {
                    (decimal)x.px,
                    (decimal)x.qty,
                });
            });

            var asks = response["ask"].ToObject<List<dynamic>>();

            asks.ForEach(x =>
            {
                orderbook.Asks.Add(new decimal[]
                {
                    (decimal)x.px,
                    (decimal)x.qty,
                });
            });

            return orderbook;
        }

        private async Task<(bool, T)> SendRequest<T>(HttpMethod method, string endpoint, object body = null,
            bool ignored400 = false)
        {
            try
            {
                string requestBody = null;

                var requestMessage = new HttpRequestMessage(method, new Uri(_baseUri, endpoint));

                if (body != null)
                {
                    requestBody = JsonConvert.SerializeObject(body);

                    requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (ignored400)
                        return (false, default);

                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Log.Warning("{client} {endpoint} {request} {response} {responseCode}", GetType().Name, endpoint,
                        requestBody, errorResponse, response.StatusCode);

                    return (false, default);
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                
                Log.Information("{client} {endpoint} {request} {response} {responseCode}", GetType().Name, endpoint,
                    requestBody, responseBody, response.StatusCode);

                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseBody);

                return (true, responseObj);
            }
            catch (Exception e)
            {
                Log.Warning(e, "{client} {endpoint} {request}", GetType().Name, endpoint, body);

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

        private async Task<string> GetSessionId()
        {
            try
            {
                var (success, response) =
                    await SendRequest<dynamic>(HttpMethod.Post, "out/api/confirm/check", new
                    {
                        acctid = _apiKey,
                        acckey = _secretKey
                    });

                if (!success || response is null)
                    return string.Empty;

                return (string)response.item.sessionId;
            }
            catch (Exception e)
            {
                Log.Error(e, "GetSessionId");
                return string.Empty;
            }
        }
    }
}