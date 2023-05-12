using mexcbot.Api.Constants;

namespace mexcbot.Api.ResponseModels.Order
{
    public class OpenOrderDto
    {
        public long BotId { get; set; }
        
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