using Newtonsoft.Json;

namespace mexcbot.Api.Models.DeepCoin
{
    public class DeepCoinError
    {
        [JsonProperty("error_code")]
        public int Code { get; set; }

        public string Description { get; set; }
    }
}