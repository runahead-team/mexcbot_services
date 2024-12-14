using mexcbot.Api.Constants;
using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore;

public class CoinStoreOrder
{
    public long Id { get; set; }
        
    public long BotId { get; set; }
        
    public BotType BotType { get; set; }

    public BotExchangeType BotExchangeType { get; set; } = BotExchangeType.COINSTORE;
        
    public long UserId { get; set; }
        
    public string Symbol { get; set; }
        
    [JsonProperty("ordId")]
    public long OrderId { get; set; }
        
    public long OrderListId { get; set; }
        
    public string Price { get; set; }
        
    public string OrigQty { get; set; }
        
    public string Type { get; set; }
        
    public string Side { get; set; }
        
    public OrderStatus Status { get; set; }
        
    public bool IsRunCancellation { get; set; }
        
    public long? ExpiredTime { get; set; }
        
    public long TransactTime { get; set; }
}