using System.Collections.Generic;
using System.Threading.Tasks;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.ResponseModels.ExchangeInfo;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.ResponseModels.Ticker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mexcbot.Api.Infrastructure.ExchangeClient
{
    public interface ExchangeClient
    {
        public  Task<ExchangeInfoView> GetExchangeInfo(string @base, string quote);

        public  Task<List<JArray>> GetCandleStick(string @base, string quote, string interval);

        public Task<Ticker24hrView> GetTicker24hr(string @base, string quote);

        public Task<OrderDto> PlaceOrder(string @base, string quote, OrderSide side,
            string quantity, string price);

        public Task<CanceledOrderView> CancelOrder(string @base, string quote, string orderId);

        public Task<List<OpenOrderView>> GetOpenOrder(string @base, string quote);

        public Task<List<AccBalance>> GetAccInformation();

        public Task<List<string>> GetSelfSymbols();

        public Task<OrderbookView> GetOrderbook(string @base, string quote);
    }
}