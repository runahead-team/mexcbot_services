using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sp.Core.Utils;

namespace mexcbot.Api.Infrastructure.Telegram
{
    public static class Telegram
    {
        private static readonly Dictionary<string, long> Messages = new();

        public static void Send(string message, uint noRepeatMinutes = 0, string messageId = "")
        {
            if (!string.IsNullOrEmpty(messageId))
            {
                if (noRepeatMinutes > 0)
                {
                    var now = AppUtils.NowMilis();
                    var mutePeriod = (long)TimeSpan.FromMinutes(noRepeatMinutes).TotalMilliseconds;


                    if (Messages.TryGetValue(messageId, out var muteTime))
                    {
                        if (muteTime > now)
                            return;

                        Messages[messageId] = now + mutePeriod;
                    }
                    else
                    {
                        Messages.Add(messageId, now + mutePeriod);
                    }
                }
            }

            Task.Run(async () =>
            {
                var payload = new
                {
                    chat_id = Configurations.Telegram.Group,
                    text = message
                };

                var httpClient = new HttpClient();

                await httpClient.PostAsync(
                    $"https://api.telegram.org/{Configurations.Telegram.Key}/sendMessage",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            });
        }
    }
}