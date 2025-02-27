using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Infrastructure.Telegram;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.ResponseModels.Order;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Serilog;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class BotMonitorJob : BackgroundService
    {
        public BotMonitorJob()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = Worker(stoppingToken);

            await Task.WhenAll(task);
        }

        private async Task Worker(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var keyValuePair in MemCache.BotStatuses)
                    {
                        if (keyValuePair.Value != "LIVE")
                            Telegram.Send($"BOT {keyValuePair.Key} stopped");
                    }
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "CancelOrderJob:CancelOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
            }
        }
    }
}