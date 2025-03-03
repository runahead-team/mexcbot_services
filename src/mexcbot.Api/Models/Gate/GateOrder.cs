using mexcbot.Api.Constants;
using Newtonsoft.Json;

namespace mexcbot.Api.Models.Gate;

public class GateOrder
{
    public long Id { get; set; }
        
    public long BotId { get; set; }
        
    public BotType BotType { get; set; }

    public BotExchangeType BotExchangeType { get; set; } = BotExchangeType.DEEPCOIN;
        
    public long UserId { get; set; }
        
    [JsonProperty("instId")]
    public string Symbol { get; set; }
        
    [JsonProperty("ordId")]
    public string OrderId { get; set; }
        
    public long OrderListId { get; set; }
        
    [JsonProperty("px")]
    public string Price { get; set; }
        
    [JsonProperty("sz")]
    public string OrigQty { get; set; }
        
    public string Type { get; set; }
        
    [JsonProperty("side")]
    public string Side { get; set; }
        
    public OrderStatus Status { get; set; }
        
    public bool IsRunCancellation { get; set; }
        
    public long? ExpiredTime { get; set; }
        
    public long TransactTime { get; set; }
}