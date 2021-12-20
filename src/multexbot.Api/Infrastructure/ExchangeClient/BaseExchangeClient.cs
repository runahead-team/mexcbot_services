using System.Collections.Generic;
using System.Threading.Tasks;
using DefaultNamespace;
using multexbot.Api.Constants;

namespace multexbot.Api.Infrastructure.ExchangeClient
{
    public abstract class BaseExchangeClient
    {
        public BaseExchangeClient()
        {
        }
        
        public abstract Task<(decimal LastPrice, decimal LastPriceUsd, decimal OpenPrice)> GetMarket(string @base,
            string quote);

        public abstract Task<OrderDto> CreateLimitOrder(string @base, string quote, decimal amount, decimal price,
            OrderSide side);

        public abstract Task<List<OrderDto>> GetOpenOrders(string @base, string quote);

        public abstract Task<bool> Cancel(string id, string uuid, string @base = null, string quote = null);

        public abstract Task<Dictionary<string, decimal>> GetFunds(params string[] coins);

        public abstract Task<OrderbookView> GetOrderbook(string @base, string quote);
    }
}