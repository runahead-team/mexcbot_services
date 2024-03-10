using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class LBankExchangeInfo
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        
        [JsonProperty("minTranQua")]
        public string MinTranQua { get; set; }
        
        [JsonProperty("quantityAccuracy")]
        public string QuantityAccuracy { get; set; }
        
        [JsonProperty("priceAccuracy")]
        public string PriceAccuracy { get; set; }
    }
}