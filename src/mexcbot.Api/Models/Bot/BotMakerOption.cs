using mexcbot.Api.Constants;

namespace mexcbot.Api.Models.Bot
{
    public class BotMakerOption
    {
        public OrderSide Side { get; set; }
        
        //Trade quantity from MinQty - MaxQty
        public bool IsRandomQty { get; set; }
        
        public decimal MinQty { get; set; }

        public decimal MaxQty { get; set; }

        public bool IsFollowBtc { get; set; }
        
        public decimal FollowBtcRate { get; set; }

        public decimal FollowBtcBasePrice { get; set; }

        public decimal FollowBtcBtcPrice { get; set; }

        public decimal MinPriceStep { get; set; }

        public decimal MaxPriceStep { get; set; }

        public decimal MinPriceOverStep { get; set; }

        public decimal MaxPriceOverStep { get; set; }

        public decimal Spread { get; set; }

        //Number of order per execute time from MinTradePerExec - MaxTradePerExec
        public int MinTradePerExec { get; set; }

        public int MaxTradePerExec { get; set; }

        //Execute interval
        public int MinInterval { get; set; }

        public int MaxInterval { get; set; }

        public int MinMatchingTime { get; set; }

        public int MaxMatchingTime { get; set; }

        //Trade bot will be terminate when price reach MinPrice/MaxPrice
        public decimal MinStopPrice { get; set; }

        public decimal MaxStopPrice { get; set; }

        //Trade bot will be terminate when balance of base reach StopLossBase
        public decimal StopLossBase { get; set; }

        //Trade bot will be terminate when balance of quote reach StopLossBase
        public decimal StopLossQuote { get; set; }

        //Order expired time: cancel order after OrderExp seconds
        public int OrderExp { get; set; }
    }
}