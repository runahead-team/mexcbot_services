using System.Collections.Generic;
using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore;

public class CoinStoreOrderbookView
{
    [JsonProperty("a")]
    public List<decimal[]> Asks { get; set; }

    [JsonProperty("b")]
    public List<decimal[]> Bids { get; set; }
}