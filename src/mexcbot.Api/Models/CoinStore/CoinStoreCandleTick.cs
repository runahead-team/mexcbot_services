using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore
{
    public class CoinStoreCandleTick
    {
        [JsonProperty("endTime")]
        public string EndTime { get; set; }
        
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("interval")]
        public string Interval { get; set; }
        
        [JsonProperty("startTime")]
        public string StartTime { get; set; }
        
        [JsonProperty("firstTradeId")]
        public string FirstTradeId { get; set; }
        
        [JsonProperty("lastTradeId")]
        public string LastTradeId { get; set; }
        
        [JsonProperty("volume")]
        public string Volume { get; set; }
        
        [JsonProperty("close")]
        public string Close { get; set; }
        
        [JsonProperty("open")]
        public string Open { get; set; }
        
        [JsonProperty("high")]
        public string High { get; set; }
        
        [JsonProperty("low")]
        public string Low { get; set; }
    }
}