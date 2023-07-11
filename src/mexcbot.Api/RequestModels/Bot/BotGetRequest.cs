using mexcbot.Api.Constants;

namespace mexcbot.Api.RequestModels.Bot
{
    public class BotGetRequest
    {
        public long Id { get; set; }
        
        public BotType Type { get; set; }
    }
}