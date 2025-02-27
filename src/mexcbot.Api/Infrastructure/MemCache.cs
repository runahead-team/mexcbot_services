using System.Collections.Generic;

namespace mexcbot.Api.Infrastructure;

public static class MemCache
{
    public static readonly Dictionary<string, string> BotStatuses = new Dictionary<string, string>();
}