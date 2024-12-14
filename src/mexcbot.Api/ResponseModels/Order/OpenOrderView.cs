using mexcbot.Api.Models.CoinStore;

namespace mexcbot.Api.ResponseModels.Order
{
    public class OpenOrderView
    {
        public OpenOrderView(){}
        
        public OpenOrderView(CoinStoreOpenOrderView openOrderView)
        {
            Symbol = openOrderView.Symbol;
            OrderId = openOrderView.OrderId.ToString();
            Price = openOrderView.Price;
            OrigQty = openOrderView.OrigQty;
            Type = openOrderView.Type;
            Side = openOrderView.Side;
            TransactTime = openOrderView.TransactTime;
        }
        
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