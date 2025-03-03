using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Models.CoinStore;
using mexcbot.Api.Models.DeepCoin;
using sp.Core.Utils;

namespace mexcbot.Api.ResponseModels.Order
{
    public class OrderDto
    {
        public OrderDto(){}
        public OrderDto(DeepCoinOrder deepCoinOrder)
        {
            Symbol = deepCoinOrder.Symbol;
            OrderId = deepCoinOrder.OrderId;
            Price = deepCoinOrder.Price;
            OrigQty = deepCoinOrder.OrigQty;
            Side = deepCoinOrder.Side;
            TransactTime = deepCoinOrder.TransactTime;
        }
        
        public OrderDto(CoinStoreOrder coinStoreOrder)
        {
            Symbol = coinStoreOrder.Symbol;
            OrderId = coinStoreOrder.OrderId.ToString();
            Price = coinStoreOrder.Price;
            OrigQty = coinStoreOrder.OrigQty;
            Side = coinStoreOrder.Side;
            TransactTime = AppUtils.NowMilis();
        }
        
        public OrderDto(Io.Gate.GateApi.Model.Order gateOrder)
        {
            Symbol = gateOrder.CurrencyPair;
            OrderId = gateOrder.Id.ToString();
            Price = gateOrder.Price;
            OrigQty = gateOrder.Amount;
            Side = BotUtils.GetSide(gateOrder.Side).ToString();
            TransactTime = AppUtils.NowMilis();
        }
        
        public long Id { get; set; }
        
        public long BotId { get; set; }
        
        public BotType BotType { get; set; }
        
        public BotExchangeType BotExchangeType { get; set; }
        
        public long UserId { get; set; }
        
        public string Symbol { get; set; }
        
        public string OrderId { get; set; }
        
        public long OrderListId { get; set; }
        
        public string Price { get; set; }
        
        public string OrigQty { get; set; }
        
        public string Type { get; set; }
        
        public string Side { get; set; }
        
        public OrderStatus Status { get; set; }
        
        public bool IsRunCancellation { get; set; }
        
        public long? ExpiredTime { get; set; }
        
        public long TransactTime { get; set; }
    }
}