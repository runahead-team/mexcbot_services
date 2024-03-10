using Newtonsoft.Json;

namespace mexcbot.Api.Models.LBank
{
    public class LBankError
    {
        [JsonProperty("error_code")]
        public int Code { get; set; }

        public string Description { get; set; }
    }
}