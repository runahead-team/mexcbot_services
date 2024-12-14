using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore;

public class CoinStoreOpenOrderView
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; }
        
    [JsonProperty("ordId")]
    public long OrderId { get; set; }
        
    public long OrderListId { get; set; }
        
    [JsonProperty("ordPrice")]
    public string Price { get; set; }
        
    [JsonProperty("ordQty")]
    public string OrigQty { get; set; }
        
    [JsonProperty("ordType")]
    public string Type { get; set; }
        
    [JsonProperty("side")]
    public string Side { get; set; }
        
    [JsonProperty("timestamp")]
    public long TransactTime { get; set; }
}