using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using DefaultNamespace;
using Microsoft.IdentityModel.Tokens;
using multexbot.Api.Constants;
using Serilog;
using sp.Core.Utils;

namespace multexbot.Api.Infrastructure.ExchangeClient
{
    public class UpbitExchangeClient : BaseExchangeClient
    {
        private readonly Uri _baseUri;
        private readonly string _apiKey;
        private readonly string _secretKey;

        private readonly HttpClient _httpClient = new HttpClient();

        public UpbitExchangeClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }

        public UpbitExchangeClient(string baseUrl, string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _baseUri = new Uri(baseUrl);
            _secretKey = secretKey;
        }

        public override async Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(
            string @base, string quote)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"v1/ticker?market={@base}-{quote}");

            if (!success || response is null)
                return (0, 0, 0);

            return (response.trade_price, response.trade_price, response.opening_price);
        }

        public override async Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Post, "v1/orders", new
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
                await SendRequest<List<dynamic>>(HttpMethod.Get, $"/v1/orders?market={@base}-{quote}&state=wait");

            if (!success || response is null)
                return new List<OrderDto>();

            return response.Select(item => new OrderDto
            {
                //Id = item.id,
                Guid = (string) item.uuid,
                Symbol = (string) item.market,
                Price = (decimal) item.price,
                Qty = (decimal) item.volume,
                Filled = (decimal) item.executed_volume,
                Total = (decimal) item.total,
                Status = ConvertOrderStatus((string) item.state)
            }).ToList();
        }

        public override async Task<bool> Cancel(string id, string @base = null, string quote = null)
        {
            var (success, response) =
                await SendRequest<object>(HttpMethod.Delete, $"v1/order?uuid={id}", null, true);

            return success;
        }

        public override async Task<Dictionary<string, decimal>> GetFunds(params string[] coins)
        {
            var (success, response) = await SendRequest<List<dynamic>>(HttpMethod.Get, "v1/accounts");

            if (!success || response is null)
                return new Dictionary<string, decimal>();

            return response.ToDictionary(item => (string) item.currency, item => (decimal) item.balance);
        }

        public override async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var (success, response) =
                await SendRequest<dynamic>(HttpMethod.Get, $"v1/orderbook?markets={@base}-{quote}");

            if (!success || response is null)
                return new OrderbookView();

            var orderbook = (List<dynamic>) response.orderbook_units;

            var bids = orderbook.Select(x => new decimal[]
            {
                x.bid_price,
                x.bid_size
            }).ToList();

            var asks = orderbook.Select(x => new decimal[]
            {
                x.ask_price,
                x.ask_size
            }).ToList();

            var result = new OrderbookView()
            {
                Bids = bids,
                Asks = asks
            };

            return result;
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
                    var jwtToken = GenerateJwtToken();
                    string authenticationToken = $"Bearer {jwtToken}";

                    requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    requestMessage.Headers.TryAddWithoutValidation("Authorization", authenticationToken);
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

                var responseObj = JsonConvert.DeserializeObject<UpbitExchangeResponse<T>>(responseBody);

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
                case "wait":
                    return OrderStatus.OPEN;
                case "watch":
                    return OrderStatus.PARTIAL_FILLED;
                case "done":
                    return OrderStatus.FILLED;
                case "cancel":
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

        private string GenerateJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var identity = new ClaimsIdentity();

            var prvKey = Convert.FromBase64String(_secretKey);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(prvKey, out _);

            var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory {CacheSignatureProviders = false}
            };

            var accessKeyClaim = new Claim("access_key", _apiKey);
            identity.AddClaim(accessKeyClaim);

            var nonceClaim = new Claim("nonce", AppUtils.NewGuidStr());
            identity.AddClaim(nonceClaim);

            SecurityToken token = tokenHandler.CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                SigningCredentials = signingCredentials,
                Subject = identity
            });

            return tokenHandler.WriteToken(token);
        }
    }

    public class UpbitExchangeResponse<T>
    {
        public bool Success { get; set; }

        public T Data { get; set; }
    }
}