using System.ComponentModel;

namespace mexcbot.Api.Constants
{
    public enum BotStatus
    {
        ACTIVE = 0,
        INACTIVE = 1
    }
    
    public enum BotType
    {
        VOLUME = 0,
        MAKER = 1
    }
    
    public enum BotExchangeType
    {
        MEXC = 0,
        LBANK = 1
    }
    
    public enum OrderSide
    {
        [Description("BUY")]
        BUY = 0,
        [Description("SELL")]
        SELL = 1,
        [Description("BOTH")]
        BOTH = 2
    }
    
    public enum OrderType
    {
        [Description("LIMIT")]
        LIMIT = 0,
        [Description("MARKET")]
        MARKET = 1,
        [Description("LIMIT_MAKER")]
        LIMIT_MAKER = 2,
        [Description("IMMEDIATE_OR_CANCEL")]
        IMMEDIATE_OR_CANCEL = 3,
        [Description("FILL_OR_KILL")]
        FILL_OR_KILL = 4
    }
    
    public enum OrderStatus
    {
        [Description("NEW")]
        NEW = 0,
        [Description("FILLED")]
        FILLED = 1,
        [Description("PARTIALLY_FILLED")]
        PARTIALLY_FILLED = 2,
        [Description("CANCELED")]
        CANCELED = 3,
        [Description("PARTIALLY_CANCELED")]
        PARTIALLY_CANCELED = 4
    }
}