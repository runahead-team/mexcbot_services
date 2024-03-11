using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.Services.Interface;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using Serilog;
using sp.Core.Extensions;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class BotCancelOrderJob : BackgroundService
    {
        public BotCancelOrderJob()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = CancelOrderJob(stoppingToken);

            await Task.WhenAll(task);
        }

        private async Task CancelOrderJob(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
                    await dbConnection.OpenAsync();

                    var orders = (await dbConnection.QueryAsync<OrderDto>(
                        "SELECT * FROM BotOrders WHERE IsRunCancellation = @IsRunCancellation AND ExpiredTime <= @Now",
                        new
                        {
                            IsRunCancellation = false,
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    var botIds = orders.Select(x => x.BotId).ToList();
                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Id IN @Ids",
                        new
                        {
                            Ids = botIds
                        })).ToList();

                    foreach (var order in orders)
                    {
                        await Execute(order, bots, dbConnection);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "BotCancelOrderJob:CancelOrderJob");
                }
            }
        }

        private async Task Execute(OrderDto order, IEnumerable<BotDto> bots, IDbConnection dbConnection)
        {
            async Task UpdateRunFlag(string orderId, bool isRunCancellation, IDbConnection dbConnection)
            {
                var updateParams = new
                {
                    OrderId = orderId,
                    IsRunCancellation = isRunCancellation
                };

                var exec = await dbConnection.ExecuteAsync(
                    "UPDATE BotOrders SET IsRunCancellation = @IsRunCancellation WHERE OrderId = @OrderId",
                    updateParams);

                if (exec != 1)
                    Log.Error("CancelOrderJob:RunExpired {@data}", updateParams);
            }

            async Task UpdateStatus(OrderDto order, IDbConnection dbConnection)
            {
                var updateParams = new
                {
                    OrderId = order.OrderId,
                    Status = order.Status,
                    ExpiredTime = (long?)null
                };

                var exec = await dbConnection.ExecuteAsync(
                    "UPDATE BotOrders SET Status = @Status WHERE OrderId = @OrderId",
                    updateParams);

                if (exec != 1)
                    Log.Error("CancelOrderJob:UpdateStatus {@data}", updateParams);
            }

            await UpdateRunFlag(order.OrderId, true, dbConnection);

            var bot = bots.FirstOrDefault(x => x.Id == order.BotId);

            if (bot == null)
                return;

            ExchangeClient client = bot.ExchangeType switch
            {
                BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey,
                    bot.ApiSecret),
                _ => throw new ArgumentOutOfRangeException()
            };

            var openOrders = await client.GetOpenOrder(bot.Base, bot.Quote);

            if (openOrders.All(x => x.OrderId != order.OrderId))
                return;

            var canceledOrder = await client.CancelOrder(bot.Base, bot.Quote, order.OrderId);

            if (canceledOrder != null)
            {
                order.Status = canceledOrder.Status;

                await UpdateStatus(order, dbConnection);
            }
        }
    }
}