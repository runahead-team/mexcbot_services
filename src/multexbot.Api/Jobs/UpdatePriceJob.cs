using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using multexbot.Api.Constants;
using multexbot.Api.Services.Interface;
using Serilog;

namespace multexbot.Api.Jobs
{
    public class UpdatePriceJob : IHostedService, IDisposable
    {
        private readonly IMarketService _marketService;
        private Timer _timer;

        public UpdatePriceJob(IMarketService marketService)
        {
            _marketService = marketService;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Log.Information("UpdatePriceJob {status}", "Running");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(MultexBotConstants.UpdateUsdPriceInterval));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("UpdatePriceJob {status}", "Stopped");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void DoWork(object state)
        {
            try
            {
                await _marketService.SysUpdatePrice();
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdatePriceJob");
            }
        }
    }
}