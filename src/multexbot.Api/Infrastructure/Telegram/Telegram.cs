using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace multexBot.Api.Infrastructure.Telegram
{
    public static class Telegram
    {
        public static void SendMessage(string message)
        {
            Task.Run(async () =>
            {
                var payload = new
                {
                    chat_id = Configurations.TelegramGroup,
                    text = $"[MultexBotclub] {message}"
                };

                var httpClient = new HttpClient();

                await httpClient.PostAsync(
                    $"https://api.telegram.org/{Configurations.TelegramBot}/sendMessage",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            });
        }
    }
}