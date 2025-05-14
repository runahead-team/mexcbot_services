using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Io.Gate.GateApi.Api;
using Io.Gate.GateApi.Client;
using Io.Gate.GateApi.Model;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Newtonsoft.Json;
using Serilog;
using Newtonsoft.Json.Linq;

namespace mexcbot.Api.Infrastructure.ExchangeClient
{
    public class GateClient : ExchangeClient
    {
        private readonly SpotApi _spotApi;

        public GateClient(string baseUrl)
        {
            var gateConfiguration = new Configuration
            {
                BasePath = baseUrl
            };

            _spotApi = new SpotApi(gateConfiguration);
        }

        public GateClient(string baseUrl, string apiKey, string secretKey)
        {
            var gateConfiguration = new Configuration
            {
                BasePath = baseUrl
            };

            gateConfiguration.SetGateApiV4KeyPair(apiKey, secretKey);
            _spotApi = new SpotApi(gateConfiguration);
        }

        public async Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";
            
            var exchangeInfo = await _spotApi.GetCurrencyPairAsync(symbol);
            
            return exchangeInfo == null ? new ExchangeInfoView() : new ExchangeInfoView(exchangeInfo);
        }

        public async Task<List<JArray>> GetCandleStick(string @base, string quote, string interval)
        {
            //10s, 1m, 5m, 15m, 30m, 60m, 4h, 8h, 1d, 7d, 30d
            var symbol = $"{@base}_{quote}";

            var candleTicks = await _spotApi.ListCandlesticksAsync(symbol, 1000, null, null, interval);

            var result = new List<JArray>();

            foreach (var candleTick in candleTicks)
            {
                // - Unix timestamp with second precision
                // - Trading volume in quote currency
                // - Closing price
                // - Highest price
                // - Lowest price
                // - Opening price
                // - Trading volume in base currency
                // - Whether the window is closed; true indicates the end of this segment of candlestick chart data, false indicates that this segment of candlestick chart data is not yet complete

                var startTimeMilis = (long.Parse(candleTick[0]) * 1000);

                result.Add([
                    startTimeMilis.ToString(),
                    candleTick[5],
                    candleTick[3],
                    candleTick[4],
                    candleTick[2],
                    candleTick[5],
                    GetEndTime(startTimeMilis, interval),
                    candleTick[1]
                ]);
            }

            return result;
        }

        public async Task<Ticker24hrView> GetTicker24hr(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";

            var tickers = await _spotApi.ListTickersAsync(symbol);

            if (tickers.Count<=0)
                return new Ticker24hrView();

            var ticker = tickers.FirstOrDefault(x =>
                string.Equals(x.CurrencyPair, symbol, StringComparison.InvariantCultureIgnoreCase));

            return ticker == null ? new Ticker24hrView() : new Ticker24hrView(ticker);
        }

        public async Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price)
        {
            var symbol = $"{@base}_{quote}";

            var gateSide = BotUtils.GetGateSide(side);
            if(gateSide==null)
                return new OrderDto();
            
            //Account types， spot - spot account, margin - margin account, unified - unified account, cross_margin - cross margin account
            var payload = new Order(null, symbol,Order.TypeEnum.Limit,"spot", gateSide.Value, quantity, price);

            var createdOrder = await _spotApi.CreateOrderAsync(payload);

            return createdOrder == null ? new OrderDto() : new OrderDto(createdOrder);
        }

        public async Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId)
        {
            //Account types， spot - spot account, margin - margin account, unified - unified account, cross_margin - cross margin account
            
            var symbol = $"{@base}_{quote}";

            if (!long.TryParse(orderId, out var orderIdRequest) || orderIdRequest <= 0)
            {
                Log.Error($"GateClient:CancelOrder: {symbol} - {orderId}");
                return new CanceledOrderView();
            }

            var cancelOrder = await _spotApi.CancelOrderAsync(orderId,symbol);

            return cancelOrder == null ? null : new CanceledOrderView(cancelOrder);
        }

        public async Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote)
        {
            var @params = $"symbol={@base}_{quote}";

            var openOrders = (await _spotApi.ListAllOpenOrdersAsync()).FirstOrDefault(x=>x.CurrencyPair == @params)?.Orders.ToList() ?? [];

            if (openOrders is not { Count: > 0 } )
                return [];

            var result = openOrders.Select(x => new OpenOrderView(x)).ToList();

            return result.Count > 0 ? result : [];
        }

        public async Task<List<AccBalance>> GetAccInformation()
        {
            var result = new List<AccBalance>();
            var accounts = await _spotApi.ListSpotAccountsAsync();

            foreach (var account in accounts)
            {
                if (!decimal.TryParse(account.Available, out var avail) || avail <= 0)
                    continue;

                result.Add(new AccBalance()
                {
                    Asset = account.Currency,
                    Free = account.Available
                });
            }

            return result;
        }

        public Task<List<string>> GetSelfSymbols()
        {
            return Task.FromResult<List<string>>([]);
        }

        public async Task<OrderbookView> GetOrderbook(string @base, string quote)
        {
            var symbol = $"{@base}_{quote}";

            var orderBook = await _spotApi.ListOrderBookAsync(symbol, "0", 1000);

            return orderBook == null ? new OrderbookView() : new OrderbookView(orderBook);
        }

        #region Private

        private long GetEndTime(long startTime, string interval)
        {
            //10s, 1m, 5m, 15m, 30m, 60m, 4h, 8h, 1d, 7d, 30d
            var endTime = interval switch
            {
                "10s" => (long)(startTime + TimeSpan.FromSeconds(10).TotalMilliseconds),
                "1m" => (long)(startTime + TimeSpan.FromMinutes(1).TotalMilliseconds),
                "5m" => (long)(startTime + TimeSpan.FromMinutes(5).TotalMilliseconds),
                "15m" => (long)(startTime + TimeSpan.FromMinutes(15).TotalMilliseconds),
                "30m" => (long)(startTime + TimeSpan.FromMinutes(30).TotalMilliseconds),
                "60m" => (long)(startTime + TimeSpan.FromMinutes(60).TotalMilliseconds),
                "4h" => (long)(startTime + TimeSpan.FromHours(4).TotalMilliseconds),
                "8h" => (long)(startTime + TimeSpan.FromHours(8).TotalMilliseconds),
                "1d" => (long)(startTime + TimeSpan.FromDays(1).TotalMilliseconds),
                "7d" => (long)(startTime + TimeSpan.FromHours(7).TotalMilliseconds),
                "30d" => (long)(startTime + TimeSpan.FromHours(30).TotalMilliseconds),
                _ => 0
            };

            return endTime;
        }

        #endregion
    }
}