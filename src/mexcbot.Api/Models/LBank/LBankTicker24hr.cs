using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class LBankTicker24hr
    {
        [JsonProperty("vol")]
        public string Vol { get; set; }
        
        [JsonProperty("high")]
        public string High { get; set; }
        
        [JsonProperty("low")]
        public string Low { get; set; }
        
        [JsonProperty("change")]
        public string Change { get; set; }
        
        [JsonProperty("turnover")]
        public string Turnover { get; set; }
        
        [JsonProperty("latest")]
        public string Latest { get; set; }
    }
}