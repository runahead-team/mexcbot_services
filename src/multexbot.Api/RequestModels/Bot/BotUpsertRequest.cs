using System.Collections.Generic;
using multexbot.Api.Constants;

namespace multexbot.Api.Models.Bot
{
    public class BotUpsertRequest : BotDto
    {
        public new BotOption Options { get; set; }

        public bool IsApiKeyChange { get; set; }
    }
}