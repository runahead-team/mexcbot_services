using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using multexbot.Api.Services.Interface;
using Serilog;

namespace multexbot.Api.Jobs
{
    public class BotJob : BackgroundService
    {
        private readonly IBotService _botService;

        public BotJob(IBotService botService)
        {
            _botService = botService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = CreateOrderJob(stoppingToken);
            var task1 = CancelExpiredOrderJob(stoppingToken);

            await Task.WhenAll(task, task1);
        }

        private async Task CreateOrderJob(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _botService.Run();

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotJob:DoWork");
                }
            }
        }

        private async Task CancelExpiredOrderJob(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _botService.CancelExpiredOrder();

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Warning(e, "VolumeBotJob:DoWork");
                }
            }
        }
    }
}