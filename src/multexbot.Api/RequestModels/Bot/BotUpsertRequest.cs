using System.Collections.Generic;
using multexBot.Api.Constants;

namespace multexBot.Api.Models.Bot
{
    public class BotUpsertRequest : BotDto
    {
        public new BotOption Options { get; set; }

        public bool IsApiKeyChange { get; set; }
    }
}