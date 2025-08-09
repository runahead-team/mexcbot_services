using Newtonsoft.Json;
using System.Collections.Generic;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitOrderbook
    {
        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("b")]
        public List<List<string>> Bids { get; set; }

        [JsonProperty("a")]
        public List<List<string>> Asks { get; set; }

        [JsonProperty("ts")]
        public long Timestamp { get; set; }

        [JsonProperty("u")]
        public long UpdateId { get; set; }
    }
}
