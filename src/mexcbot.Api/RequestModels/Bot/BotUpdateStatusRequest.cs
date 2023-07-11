using mexcbot.Api.Constants;
using mexcbot.Api.Models.Bot;

namespace mexcbot.Api.RequestModels.Bot
{
    public class BotUpdateStatusRequest
    {
        public long Id { get; set; }

        public BotStatus Status { get; set; }
    }
}