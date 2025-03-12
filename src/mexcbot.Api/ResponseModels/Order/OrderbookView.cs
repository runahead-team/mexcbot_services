using System.Collections.Generic;
using System.Linq;
using Io.Gate.GateApi.Model;
using mexcbot.Api.Models.CoinStore;
using sp.Core.Utils;

namespace mexcbot.Api.ResponseModels.Order
{
    public class OrderbookView
    {
        public OrderbookView()
        {
            Ver = AppUtils.NowMilis();
            Asks = new List<decimal[]>();
            Bids = new List<decimal[]>();
        }

        public OrderbookView(CoinStoreOrderbookView orderbookView)
        {
            Ver = AppUtils.NowMilis();
            Asks = orderbookView.Asks;
            Bids = orderbookView.Bids;
        }
        
        public OrderbookView(OrderBook gateOrderbookView)
        {
            Ver = AppUtils.NowMilis();
            Asks = gateOrderbookView.Asks.Select(x => x.Select(decimal.Parse).ToArray()).ToList();
            Bids = gateOrderbookView.Bids.Select(x => x.Select(decimal.Parse).ToArray()).ToList();
        }

        public long Ver { get; set; }

        public long Timestamp => Ver;

        public List<decimal[]> Asks { get; set; }

        public List<decimal[]> Bids { get; set; }
    }
}