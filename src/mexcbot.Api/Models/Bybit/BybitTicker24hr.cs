using Newtonsoft.Json;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitTicker24hr
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; }

        [JsonProperty("prevPrice24h")]
        public string PrevPrice24h { get; set; }

        [JsonProperty("price24hPcnt")]
        public string Price24hPcnt { get; set; }

        [JsonProperty("highPrice24h")]
        public string HighPrice24h { get; set; }

        [JsonProperty("lowPrice24h")]
        public string LowPrice24h { get; set; }

        [JsonProperty("volume24h")]
        public string Volume24h { get; set; }

        [JsonProperty("turnover24h")]
        public string Turnover24h { get; set; }
    }
}
