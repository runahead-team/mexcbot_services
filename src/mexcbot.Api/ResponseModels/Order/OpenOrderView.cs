namespace mexcbot.Api.ResponseModels.Order
{
    public class OpenOrderView
    {
        public string Symbol { get; set; }
        
        public string OrderId { get; set; }
        
        public long OrderListId { get; set; }
        
        public string Price { get; set; }
        
        public string OrigQty { get; set; }
        
        public string Type { get; set; }
        
        public string Side { get; set; }
        
        public long TransactTime { get; set; }
    }
}