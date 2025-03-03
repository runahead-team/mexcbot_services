using System.Collections.Generic;
using mexcbot.Api.Models.Bot;

namespace mexcbot.Api.Infrastructure;

public static class MemCache
{
    public static readonly Dictionary<string, string> ActiveBots = new Dictionary<string, string>();


    public static void AddActiveBot(BotDto bot)
    {
        var key = $"{bot.Symbol}-{bot.ExchangeType}-#{bot.Id}".ToUpper();
        if (!ActiveBots.ContainsKey(key))
            ActiveBots.TryAdd(key, "ACTIVE");
    }

    public static readonly Dictionary<string, string> LiveBots = new Dictionary<string, string>();


    public static void AddLiveBot(BotDto bot)
    {
        var key = $"{bot.Symbol}-{bot.ExchangeType}-#{bot.Id}".ToUpper();
        if (!LiveBots.ContainsKey(key))
            LiveBots.TryAdd(key, "ACTIVE");
    }
}