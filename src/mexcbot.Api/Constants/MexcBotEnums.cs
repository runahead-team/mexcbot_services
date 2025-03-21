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
        LBANK = 1,
        UZX = 2,
        DEEPCOIN = 3,
        COINSTORE = 4,
        BYBIT = 5,
        OKX = 6,
        BITGET = 7,
        KUCOIN = 8,
        HTX = 9,
        GATE = 10,
        BINGX = 11,
        CRYPTO = 12,
        KRAKEN = 13,
        UPBIT = 14,
        BINANCE = 15
    }

    public enum OrderSide
    {
        [Description("BUY")] BUY = 0,
        [Description("SELL")] SELL = 1,
        [Description("BOTH")] BOTH = 2,
        [Description("OB")] OB = 3
    }

    public enum OrderType
    {
        [Description("LIMIT")] LIMIT = 0,
        [Description("MARKET")] MARKET = 1,
        [Description("LIMIT_MAKER")] LIMIT_MAKER = 2,
        [Description("IMMEDIATE_OR_CANCEL")] IMMEDIATE_OR_CANCEL = 3,
        [Description("FILL_OR_KILL")] FILL_OR_KILL = 4
    }

    public enum OrderStatus
    {
        [Description("NEW")] NEW = 0,
        [Description("FILLED")] FILLED = 1,
        [Description("PARTIALLY_FILLED")] PARTIALLY_FILLED = 2,
        [Description("CANCELED")] CANCELED = 3,
        [Description("PARTIALLY_CANCELED")] PARTIALLY_CANCELED = 4,
        [Description("UNFILLED")] UNFILLED = 5,
        [Description("UNKNOWN")] UNKNOWN = -2,

        //CoinStore
        [Description("NOT_FOUND")] NOT_FOUND = -3,
        [Description("SUBMITTING")] SUBMITTING = 6,
        [Description("SUBMITTED")] SUBMITTED = 7,
    }
}