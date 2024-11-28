using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class DeepCoinTicker24hr
    {
        [JsonProperty("instId")]
        public string Symbol { get; set; }
        
        [JsonProperty("vol24h")]
        public string Vol { get; set; }
        
        [JsonProperty("high24h")]
        public string High { get; set; }
        
        [JsonProperty("low24h")]
        public string Low { get; set; }
        
        [JsonProperty("volCcy24h")]
        public string Turnover { get; set; }
        
        [JsonProperty("last")]
        public string Latest { get; set; }
    }
}