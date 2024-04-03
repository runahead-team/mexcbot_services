using mexcbot.Api.Constants;

namespace mexcbot.Api.Models.Bot
{
    public class BotVolumeOption
    {
        public OrderSide Side { get; set; }
        
        public decimal Volume24hr { get; set; }
        
        public long MatchingDelayFrom { get; set; }
        
        public long MatchingDelayTo { get; set; }
        
        public decimal MinOrderQty { get; set; }
        
        public decimal MaxOrderQty { get; set; }
        
        //Execute interval
        public int MinInterval { get; set; }

        public int MaxInterval { get; set; }
        
        public bool AlwaysRun { get; set; }
        
        public bool SafeRun { get; set; }
    }
}