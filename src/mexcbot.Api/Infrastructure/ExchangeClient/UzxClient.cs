using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Mexc;
using Newtonsoft.Json;
using Serilog;

namespace mexcbot.Api.Infrastructure.ExchangeClient;

public class UzxClient
{
    private readonly HttpClient _httpClient = new();

    public UzxClient(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-uzx-pro-api-key", apiKey);
    }

    public async Task<List<UzxSymbolThumb>> GetSymbolThumb()
    {
        var (success, responseBody) =
            await SendRequest("GET", "https://api.uzx.com/market/symbol-thumb", null, true);

        if (!success)
            return null;

        return JsonConvert.DeserializeObject<List<UzxSymbolThumb>>(responseBody);
    }

    public async Task<UzxOrder> CreateOrder(string @base, string quote, decimal price, decimal qty, OrderSide side)
    {
        var symbol = $"{@base}{quote}";

        var (success, responseBody) =
            await SendRequest("POST", "https://api.t-uzx.com/exchange/api/add-order", new UzxCreateOrderRequest
            {
                Symbol = symbol,
                Direction = side.ToString(),
                Price = price,
                Amount = qty,
                Type = "LIMIT_PRICE"
            }, true);

        if (!success)
            return null;

        return JsonConvert.DeserializeObject<UzxOrder>(responseBody);
    }

    public async Task<bool> CancelOrder(string orderId)
    {
        var (success, responseBody) =
            await SendRequest("POST", "https://api.t-uzx.com/exchange/api/cancel-order", new UzxCancelOrderRequest
            {
                OrderId = orderId
            }, true);

        return success;
    }

    private async Task<(bool, string)> SendRequest(string method, string endpoint, object payload, bool logInfo = true)
    {
        var uri = new Uri(endpoint);

        if (logInfo)
            Log.Information($"UzxClient:SendRequest request {endpoint} {payload}");


        HttpContent content = null;

        if (payload != null)
        {
            var jsonBody = JsonConvert.SerializeObject(payload);
            content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        string responseBody = null;

        try
        {
            HttpResponseMessage response;

            switch (method)
            {
                case "GET":
                    response = await _httpClient.GetAsync(uri);
                    break;
                case "POST":
                    response = await _httpClient.PostAsync(uri, content);
                    break;
                case "PUT":
                    response = await _httpClient.PutAsync(uri, content);
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
                    Log.Information($"UzxClient:SendRequest response {endpoint} {payload} {responseBody}");

                return (true, responseBody);
            }

            responseBody = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<MexcError>(responseBody);

            Log.Error($"UzxClient:SendRequest response {endpoint} {payload} {responseBody}");

            var errorMessage = $"{error.Code} {error.Description}";

            return (false, errorMessage);
        }
        catch (Exception e)
        {
            if (e is TaskCanceledException)
                Log.Error("UzxClient request timeout {uri} {response}", uri, responseBody);
            else if (e is HttpRequestException)
                Log.Error("UzxClient http error {uri} {response}", uri, responseBody);
            else
                Log.Error(e, "UzxClient {response}", responseBody);

            return (false, string.Empty);
        }
    }
}

public class UzxSymbolThumb
{
    public string Symbol { get; set; }

    [JsonProperty("coinScale")] public int BasePrecision { get; set; }

    [JsonProperty("baseCoinScale")] public int QuotePrecision { get; set; }

    [JsonProperty("close")] public decimal LastPrice { get; set; }
}

public class UzxCreateOrderRequest
{
    public string Symbol { get; set; }

    //BUY, SELL
    public string Direction { get; set; }

    public decimal Price { get; set; }

    public decimal Amount { get; set; }

    //LIMIT_PRICE
    public string Type { get; set; }
}

public class UzxOrder
{
    public string OrderId { get; set; }
}

public class UzxCancelOrderRequest
{
    public string OrderId { get; set; }
}