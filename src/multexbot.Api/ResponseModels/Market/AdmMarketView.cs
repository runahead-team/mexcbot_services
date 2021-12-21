using multexbot.Api.Models.Market;

namespace multexbot.Api.ResponseModels.Market
{
    public class AdmMarketView
    {
        public AdmMarketView()
        {
        }

        public AdmMarketView(MarketDto market)
        {
            Coin = market.Coin;
            UsdPrice = market.UsdPrice;
            IsActive = market.IsActive;
            PriceUpdatedTime = market.PriceUpdatedTime;
        }
        
        public string Coin { get; set; }

        public decimal UsdPrice { get; set; }

        public bool IsActive { get; set; }

        public long PriceUpdatedTime { get; set; }
    }
}