using mexcbot.Api.Constants;

namespace mexcbot.Api.Models.Bot
{
    public class BotDto
    {
        public long Id { get; set; }
        
        public long UserId { get; set; }

        public string Base { get; set; }
        
        public string Quote { get; set; }
        
        public string Symbol => $"{Base}{Quote}";
        
        public decimal Volume24hr { get; set; }
        
        public long MatchingDelayFrom { get; set; }
        
        public long MatchingDelayTo { get; set; }
        
        public decimal MinOrderQty { get; set; }
        
        public decimal MaxOrderQty { get; set; }
        
        public string ApiKey { get; set; }
        
        public string ApiSecret { get; set; }
        
        public string Logs { get; set; }

        public BotStatus Status { get; set; }

        public long LastRunTime { get; set; }
        
        public long CreatedTime { get; set; }
    }
}