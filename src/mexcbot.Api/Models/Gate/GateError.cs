using Newtonsoft.Json;

namespace mexcbot.Api.Models.Gate
{
    public class GateError
    {
        [JsonProperty("error_code")]
        public int Code { get; set; }

        public string Description { get; set; }
    }
}