using System;
using Io.Gate.GateApi.Model;
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
        
        public OpenOrderView(Io.Gate.GateApi.Model.Order openOrderView)
        {
            Symbol = openOrderView.CurrencyPair;
            OrderId = openOrderView.Id;
            Price = openOrderView.Price;
            OrigQty = openOrderView.Amount;
            Type = openOrderView.Type.HasValue ? openOrderView.Type.Value.ToString() : string.Empty;
            Side =  openOrderView.Side.ToString();
            TransactTime = openOrderView.CreateTimeMs;
        }
        
        public string Symbol { get; set; }
        
        public string OrderId { get; set; }
        
        public long OrderListId { get; set; }
        
        public string Price { get; set; }
        
        public string OrigQty { get; set; }
        
        public string Type { get; set; }
        
        public string Side { get; set; }
        
        public string ExecutedQty { get; set; }
        
        public string Status { get; set; }
        
        public long Time { get; set; }
        
        public long TransactTime { get; set; }
    }
}