using Newtonsoft.Json;

namespace mexcbot.Api.Models.DeepCoin
{
    public class DeepCoinAccBalance
    {
        [JsonProperty("ccy")]
        public string Asset { get; set; }
        
        [JsonProperty("availBal")]
        public string Free { get; set; }
        
        [JsonProperty("bal")]
        public string Balance { get; set; }
        
        [JsonProperty("frozenBal")]
        public string Frozen { get; set; }
    }
}