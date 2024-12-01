using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class DeepCoinExchangeInfo
    {
        [JsonProperty("instId")]
        public string Symbol { get; set; }
        
        //Min Order Quantity
        [JsonProperty("minSz")]
        public string MinSz { get; set; }
        
        //"0.00000001" => Precision = 8 (BasePrecision)
        [JsonProperty("lotSz")]
        public string LotSz { get; set; }
        
        //"0.01" => Precision = 2 (QuotePrecision)
        [JsonProperty("tickSz")]
        public string TickSz { get; set; }
        
        //live, suspend, preopen
        [JsonProperty("state")]
        public string State { get; set; }
        
        [JsonProperty("MaxLmtSz")]
        public string MaxLimitSz { get; set; }
    }
}