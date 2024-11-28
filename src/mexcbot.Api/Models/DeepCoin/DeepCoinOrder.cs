using mexcbot.Api.Constants;
using Newtonsoft.Json;

namespace mexcbot.Api.Models.DeepCoin;

public class DeepCoinOrder
{
    public long Id { get; set; }
        
    public long BotId { get; set; }
        
    public BotType BotType { get; set; }
        
    public BotExchangeType BotExchangeType { get; set; }
        
    public long UserId { get; set; }
        
    [JsonProperty("instId")]
    public string Symbol { get; set; }
        
    [JsonProperty("clOrdId")]
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