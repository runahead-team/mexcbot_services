using Newtonsoft.Json;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitAccBalance
    {
        [JsonProperty("coin")]
        public string Asset { get; set; }

        [JsonProperty("walletBalance")]
        public string Total { get; set; }

        [JsonProperty("equity")]
        public string Free { get; set; }
    }
}
