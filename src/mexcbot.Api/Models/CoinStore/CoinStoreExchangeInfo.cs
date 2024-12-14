using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class CoinStoreExchangeInfo
    {
        [JsonProperty("symbolCode")]
        public string Symbol { get; set; }
        
        //"0.00000001" => Precision = 8 (BasePrecision)
        [JsonProperty("lotSz")]
        public string LotSz { get; set; }
        
        //"0.01" => Precision = 2 (QuotePrecision)
        [JsonProperty("tickSz")]
        public string TickSz { get; set; }
        
        //Min Order Price
        [JsonProperty("minLmtPr")]
        public string MinLimitPrice { get; set; }
        
        //Min Order Price
        [JsonProperty("minLmtSz")]
        public string MinLimitSize { get; set; }
        
        [JsonProperty("openTrade")]
        public bool OpenTrade { get; set; }
        
        public string State => OpenTrade == true ? "OpenTrade" : "CloseTrade";
    }
}