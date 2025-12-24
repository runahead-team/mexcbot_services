using mexcbot.Api.Constants;
using mexcbot.Api.Models.Bot;

namespace mexcbot.Api.RequestModels.Bot
{
    public class BotUpsertRequest : BotDto
    {
        public new BotVolumeOption VolumeOption { get; set; }

        public new BotMakerOption MakerOption { get; set; }

        public new BotLiqOption LiqOption { get; set; }
    }
}