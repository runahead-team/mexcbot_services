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
using multexBot.Api.Constants;
using Serilog;

namespace multexBot.Api.Infrastructure.ExchangeClient
{
    public class SpExchangeClient : BaseExchangeClient
    {
        private readonly string _secretKey;
        private readonly Uri _baseUri;


        private readonly HttpClient _httpClient = new HttpClient();

        public SpExchangeClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public SpExchangeClient(string baseUrl, string apiKey, string secretKey)
        {
            _secretKey = secretKey;
            _baseUri = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("SPEX-APIKEY", apiKey);
        }

        public override async Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(
            string @base, string quote)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"public-api/get-market?symbol={@base + quote}");

            if (!success || response is null)
                return (0, 0, 0);

            return (response.lastPrice, response.lastPriceUsd, response.open);
        }

        public override async Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, "public-api/create-order", new
                {
                    symbol = $"{@base}{quote}",
                    qty = amount,
                    price = price,
                    side = side.ToString("G"),
                    type = "LIMIT"
                });

            if (!success)
                return null;

            return new OrderDto
            {
                ExternalId = response.id,
                Symbol = (string) response.symbol,
                Base = @base,
                Quote = quote,
                Price = (decimal) response.price,
                Side = side,
                Qty = (decimal) response.qty,
                Filled = (decimal) response.filled,
                Total = (decimal) response.total,
                Status = ConvertOrderStatus((string) response.status)
            };
        }

        public override async Task<List<OrderDto>> GetOpenOrders(string @base, string quote)
        {
            var (success, response) =
                await SendRequest<List<dynamic>>(HttpMethod.Get, $"public-api/get-open-order?symbol={@base + quote}");

            if (!success || response is null)
                return new List<OrderDto>();

            return response.Select(item => new OrderDto
            {
                Id = item.id,
                Symbol = (string) item.symbol,
                Price = (decimal) item.price,
                Qty = (decimal) item.qty,
                Filled = (decimal) item.filled,
                Total = (decimal) item.total,
                Status = ConvertOrderStatus((string) item.status)
            }).ToList();
        }

        public override async Task<bool> Cancel(string id, string uuid, string @base = null, string quote = null)
        {
            var (success, response) =
                await SendRequest<object>(HttpMethod.Delete, $"public-api/cancel-order?id={id}", null, true);

            return success;
        }

        public override async Task<Dictionary<string, decimal>> GetFunds(params string[] coins)
        {
            var (success, response) = await SendRequest<List<dynamic>>(HttpMethod.Get, "public-api/get-fund");

            if (!success || response is null)
                return new Dictionary<string, decimal>();

            return response.ToDictionary(item => (string) item.coin, item => (decimal) item.amount);
        }

        public override async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var (success, response) =
                await SendRequest<OrderbookView>(HttpMethod.Get, $"public-api/get-orderbook?symbol={@base + quote}");

            if (!success || response is null)
                return new OrderbookView();

            return response;
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
                    var signature = HMAC_SHA256(requestBody, _secretKey);

                    requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    requestMessage.Headers.TryAddWithoutValidation("SPEX-SIGNATURE", signature);
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

                var responseObj = JsonConvert.DeserializeObject<SpExchangeResponse<T>>(responseBody);

                if (!responseObj.Success)
                    return (false, default);

                return (true, responseObj.Data);
            }
            catch (Exception e)
            {
                Log.Warning(e, "{client} {endpoint} {request}", GetType().Name, endpoint, body);

                return (false, default);
            }
        }

        private OrderStatus ConvertOrderStatus(string status)
        {
            switch (status)
            {
                case "OPEN":
                    return OrderStatus.OPEN;
                case "PARTIALLY_FILLED":
                    return OrderStatus.PARTIAL_FILLED;
                case "FILLED":
                    return OrderStatus.FILLED;
                case "CANCELED":
                case "REJECTED":
                case "EXPIRED":
                    return OrderStatus.CANCELED;
                default:
                    return OrderStatus.CANCELED;
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

    public class SpExchangeResponse<T>
    {
        public bool Success { get; set; }

        public T Data { get; set; }
    }
}