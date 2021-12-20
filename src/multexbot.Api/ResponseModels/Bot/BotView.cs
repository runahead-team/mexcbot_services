using System.Collections.Generic;
using multexbot.Api.Constants;
using multexbot.Api.Models.Bot;
using multexbot.Api.ResponseModels.PriceOption;
using Newtonsoft.Json;

namespace multexbot.Api.Models.Bot
{
    public class BotView
    {
        public BotView(){}

        public BotView(BotDto bot)
        {
            Id = bot.Id;
            Guid = bot.Guid;
            UserId = bot.UserId;
            Email = bot.Email;
            Name = bot.Name;
            ExchangeType = bot.ExchangeType;
            ApiKey = !string.IsNullOrEmpty(bot.ApiKey) && bot.ApiKey.Length > 5 ? bot.ApiKey.Substring(0, 5) + "*****" : "*****";
            SecretKey = "*****";
            RootId = bot.RootId;
            IsActive = bot.IsActive;
            Options = JsonConvert.DeserializeObject<BotOption>(bot.Options);
        }
        
        public long Id { get; set; }

        public string Guid { get; set; }

        public long UserId { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public ExchangeType ExchangeType { get; set; }

        public string ApiKey { get; set; }

        public string SecretKey { get; set; }

        public long? RootId { get; set; }
        
        public bool IsActive { get; set; }

        //Json Serialize of BotOption
        public BotOption Options { get; set; }
        
        public List<PriceOptionView> PriceOptions { get; set; }
    }
}