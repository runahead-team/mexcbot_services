using mexcbot.Api.Constants;

namespace mexcbot.Api.ResponseModels.Order
{
    public class CanceledOrderView
    {
        public string Symbol { get; set; }
        
        public string OrigClientOrderId { get; set; }
        
        public string OrderId { get; set; }
        
        public string ClientOrderId { get; set; }
        
        public string Price { get; set; }
        
        public string OrigQty { get; set; }
        
        public string ExecutedQty { get; set; }
        
        public string CummulativeQuoteQty { get; set; }
        
        public OrderStatus Status { get; set; }

        public long TimeInForce { get; set; }
        
        public string Type { get; set; }
        
        public string Side { get; set; }
    }
}