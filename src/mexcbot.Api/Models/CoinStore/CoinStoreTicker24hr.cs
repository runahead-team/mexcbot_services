using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class CoinStoreTicker24hr
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        
        [JsonProperty("volume")]
        public string Vol { get; set; }
        
        [JsonProperty("high")]
        public string High { get; set; }
        
        [JsonProperty("low")]
        public string Low { get; set; }
        
        [JsonProperty("amount")]
        public string Turnover { get; set; }
        
        [JsonProperty("close")]
        public string Latest { get; set; }
    }
}