using System.Collections.Generic;
using mexcbot.Api.Models.Bot;
using sp.Core.Utils;

namespace mexcbot.Api.Infrastructure;

public static class MemCache
{
    public static readonly Dictionary<string, long> ActiveBots = new Dictionary<string, long>();


    public static void AddActiveBot(BotDto bot)
    {
        var now = AppUtils.NowMilis();
        var key = $"{bot.Symbol}-{bot.ExchangeType}-#{bot.Id}".ToUpper();
        if (!ActiveBots.ContainsKey(key))
            ActiveBots.TryAdd(key, now);
        else
            ActiveBots[key] = now;
    }

    public static void RemoveActiveBot(BotDto bot)
    {
        var key = $"{bot.Symbol}-{bot.ExchangeType}-#{bot.Id}".ToUpper();
        ActiveBots.Remove(key);
    }

    public static readonly Dictionary<string, long> LiveBots = new Dictionary<string, long>();


    public static void AddLiveBot(BotDto bot)
    {
        var now = AppUtils.NowMilis();
        var key = $"{bot.Symbol}-{bot.ExchangeType}-#{bot.Id}".ToUpper();

        if (!LiveBots.ContainsKey(key))
            LiveBots.TryAdd(key, now);
        else
            LiveBots[key] = now;
    }
}