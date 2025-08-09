using Newtonsoft.Json;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitError
    {
        [JsonProperty("retCode")]
        public int Code { get; set; }

        [JsonProperty("retMsg")]
        public string Description { get; set; }
    }
}
