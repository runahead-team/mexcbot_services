namespace mexcbot.Api.Models.Order
{
    public class OrderbookView
    {
        public OrderbookView()
        {
            Ver = AppUtils.NowMilis();
            Asks = new List<decimal[]>();
            Bids = new List<decimal[]>();
        }

        public long Ver { get; set; }

        public long Timestamp => Ver;

        public List<decimal[]> Asks { get; set; }

        public List<decimal[]> Bids { get; set; }
    }
}