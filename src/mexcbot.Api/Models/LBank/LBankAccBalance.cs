using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class LBankAccBalance
    {
        [JsonProperty("asset")]
        public string Asset { get; set; }
        
        [JsonProperty("free")]
        public string Free { get; set; }
    }
}