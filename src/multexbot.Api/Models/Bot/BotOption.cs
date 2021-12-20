namespace multexbot.Api.Models.Bot
{
    public class BotOption
    {
        //Trade quantity from MinQty - MaxQty
        public decimal MinQty { get; set; }

        public decimal MaxQty { get; set; }

        public bool RandomQty { get; set; }

        //Price increase each execute time from MinPriceStep - MaxPriceStep

        public bool LastPrice { get; set; }

        public bool FollowBtc { get; set; }

        public decimal FollowBtcBasePrice { get; set; }

        public decimal FollowBtcBtcPrice { get; set; }


        public decimal BasePrice { get; set; }

        public decimal MinPriceStep { get; set; }

        public decimal MaxPriceStep { get; set; }

        public decimal MinPriceOverStep { get; set; }

        public decimal MaxPriceOverStep { get; set; }

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

        //Fix to round
        public int PriceFix { get; set; }

        public int QtyFix { get; set; }
    }
}