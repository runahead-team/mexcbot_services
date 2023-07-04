using mexcbot.Api.Constants;
using mexcbot.Api.Models.Bot;

namespace mexcbot.Api.RequestModels.Bot
{
    public class BotUpdateRequest
    {
        public long Id { get; set; }
        
        public string Base { get; set; }
        
        public string Quote { get; set; }
        
        public string Symbol => $"{Base}{Quote}";
        
        public BotType Type { get; set; }
        
        public BotVolumeOption VolumeOption { get; set; }

        public BotStatus Status { get; set; }
    }
}