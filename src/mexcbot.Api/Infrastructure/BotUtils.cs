using Io.Gate.GateApi.Model;
using mexcbot.Api.Constants;

namespace mexcbot.Api.Infrastructure;

public static class BotUtils
{
    public static OrderStatus GetStatus(Order.StatusEnum? gateStatus)
    {
        var status = gateStatus switch
        {
            Order.StatusEnum.Open => OrderStatus.NEW,
            Order.StatusEnum.Closed => OrderStatus.FILLED,
            Order.StatusEnum.Cancelled => OrderStatus.CANCELED,
            _ => OrderStatus.UNKNOWN
        };

        return status;
    }
    
    public static OrderSide GetSide(Order.SideEnum gateSide)
    {
        var side = gateSide switch
        {
            Order.SideEnum.Buy => OrderSide.BUY,
            Order.SideEnum.Sell => OrderSide.SELL,
            _ => OrderSide.OB
        };

        return side;
    }
    
    public static Order.SideEnum? GetGateSide(OrderSide orderSide)
    {
        var side = orderSide switch
        {
            OrderSide.BUY => Order.SideEnum.Buy,
            OrderSide.SELL => Order.SideEnum.Sell,
            _ => (Order.SideEnum?)null
        };

        return side;
    }
}