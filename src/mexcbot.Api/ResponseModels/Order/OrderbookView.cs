using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Io.Gate.GateApi.Model;
using mexcbot.Api.Models.CoinStore;
using mexcbot.Api.Models.Bybit;
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
            Asks = gateOrderbookView.Asks.Select(x => x.Select(y => decimal.Parse(y, new NumberFormatInfo())).ToArray())
                .ToList();
            Bids = gateOrderbookView.Bids.Select(x => x.Select(y => decimal.Parse(y, new NumberFormatInfo())).ToArray()).ToList();
        }

        public OrderbookView(BybitOrderbook bybitOrderbook)
        {
            Ver = AppUtils.NowMilis();
            Asks = bybitOrderbook.Asks.Select(x => x.Select(y => decimal.Parse(y, new NumberFormatInfo())).ToArray())
                .ToList();
            Bids = bybitOrderbook.Bids.Select(x => x.Select(y => decimal.Parse(y, new NumberFormatInfo())).ToArray()).ToList();
        }

        public long Ver { get; set; }

        public long Timestamp => Ver;

        public List<decimal[]> Asks { get; set; }

        public List<decimal[]> Bids { get; set; }
    }
}