using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore
{
    public class CoinStoreError
    {
        [JsonProperty("error_code")]
        public int Code { get; set; }

        public string Description { get; set; }
    }
}