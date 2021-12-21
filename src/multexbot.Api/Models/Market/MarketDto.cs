using multexbot.Api.RequestModels.Market;
using sp.Core.Utils;

namespace multexbot.Api.Models.Market
{
    public class MarketDto
    {
        public string Coin { get; set; }
        
        public decimal UsdPrice { get; set; }
        
        public bool IsActive { get; set; }
        
        public long PriceUpdatedTime { get; set; }
    }
}