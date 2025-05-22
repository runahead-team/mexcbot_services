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
using mexcbot.Api.Models.Bot;
using mexcbot.Api.ResponseModels.Order;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Serilog;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class CancelOrderJob : BackgroundService
    {
        public CancelOrderJob()
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
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                    var orders = (await dbConnection.QueryAsync<OrderDto>(
                        "SELECT * FROM BotOrders WHERE IsRunCancellation = @IsRunCancellation AND ExpiredTime <= @Now LIMIT 30",
                        new
                        {
                            IsRunCancellation = false,
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    var botIds = orders.Select(x => x.BotId).ToList();
                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE `Id` IN @Ids",
                        new
                        {
                            Ids = botIds
                        })).ToList();

                    var tasks = new List<Task>();

                    foreach (var order in orders)
                    {
                        tasks.Add(Execute(order, bots));
                    }

                    await Task.WhenAll(tasks);

                    #region Delete orders 7 days ago

                    var ago2Days = AppUtils.NowMilis() - TimeSpan.FromDays(2).TotalMilliseconds;
                    await dbConnection.QueryAsync<OrderDto>(
                        "DELETE FROM BotOrders WHERE `IsRunCancellation` = @IsRunCancellation AND `ExpiredTime` <= @Ago7Days",
                        new
                        {
                            IsRunCancellation = true,
                            Ago7Days = ago2Days
                        });

                    #endregion

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    Log.Error(e, "CancelOrderJob:CancelOrderJob");

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task Execute(OrderDto order, IEnumerable<BotDto> bots)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            async Task UpdateRunFlag(string orderId, bool isRunCancellation)
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

            async Task UpdateStatus(OrderDto order)
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

            await UpdateRunFlag(order.OrderId, true);

            var bot = bots.FirstOrDefault(x => x.Id == order.BotId);

            if (bot == null)
                return;

            MemCache.AddLiveBot(bot);

            ExchangeClient client = bot.ExchangeType switch
            {
                BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey,
                    bot.ApiSecret),
                BotExchangeType.DEEPCOIN => new DeepCoinClient(Configurations.DeepCoinUrl, bot.ApiKey,
                    bot.ApiSecret, bot.Passphrase),
                BotExchangeType.COINSTORE =>
                    new CoinStoreClient(Configurations.CoinStoreUrl, bot.ApiKey, bot.ApiSecret),
                BotExchangeType.GATE => new GateClient(Configurations.GateUrl, bot.ApiKey,
                    bot.ApiSecret),
                _ => null
            };

            if (client == null)
                return;

            var canceledOrder = await client.CancelOrder(bot.Base, bot.Quote, order.OrderId);

            if (canceledOrder == null &&
                new[] { BotExchangeType.MEXC, BotExchangeType.GATE }.Contains(bot.ExchangeType))
            {
                order.Status = OrderStatus.CANCELED;
                await UpdateStatus(order);
            }
            else if (canceledOrder != null && (!string.IsNullOrEmpty(canceledOrder.Symbol) ||
                                               !string.IsNullOrEmpty(canceledOrder.OrderId)))
            {
                //-1: Cancelled 0: Unfilled 1: Partially filled 2: Completely filled 3: Partially filled has been cancelled 4: Cancellation is being processed
                if (bot.ExchangeType == BotExchangeType.LBANK)
                    order.Status = canceledOrder.LbankOrderStatus switch
                    {
                        "-1" => OrderStatus.CANCELED,
                        "2" => OrderStatus.PARTIALLY_FILLED,
                        "3" => OrderStatus.PARTIALLY_CANCELED,
                        "0" => OrderStatus.UNFILLED,
                        _ => OrderStatus.UNKNOWN
                    };
                else
                    order.Status = bot.ExchangeType is BotExchangeType.DEEPCOIN
                        ? OrderStatus.CANCELED
                        : canceledOrder.Status;

                await UpdateStatus(order);
            }
        }
    }
}