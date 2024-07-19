using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Newtonsoft.Json;
using Serilog;
using sp.Core.Extensions;
using sp.Core.Utils;

namespace mexcbot.Api.Jobs
{
    public class UzxMarketMarkerJob : BackgroundService
    {
        public UzxMarketMarkerJob()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Worker(stoppingToken);
        }

        private async Task Worker(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                    var bots = (await dbConnection.QueryAsync<BotDto>(
                        "SELECT * FROM Bots WHERE Status = @Status AND Type = @Type AND ExchangeType = @ExchangeType AND (NextRunMakerTime < @Now OR NextRunMakerTime IS NULL)",
                        new
                        {
                            Status = BotStatus.ACTIVE,
                            Type = BotType.MAKER,
                            ExchangeType = BotExchangeType.UZX,
                            Now = AppUtils.NowMilis()
                        })).ToList();

                    if (bots.Count == 0)
                        continue;

                    var tasks = bots.Select(Run).ToList();

                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                        Log.Error(e, "UzxMarketMarkerJob:CreateOrderJob");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task Run(BotDto bot)
        {
            var stopLog = "";
            var now = AppUtils.NowMilis();

            try
            {
                Log.Information("BOT {0} run", bot.Symbol);

                var client = new UzxClient(bot.ApiKey);

                await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                #region Cancel Orders

                var orders = (await dbConnection.QueryAsync<OrderDto>(
                    "SELECT * FROM BotOrders WHERE `BotId` = @BotId AND `IsRunCancellation` = @IsRunCancellation AND `ExpiredTime` <= @Now",
                    new
                    {
                        BotId = bot.Id,
                        IsRunCancellation = false,
                        Now = AppUtils.NowMilis()
                    })).ToList();

                if (orders.Count > 0)
                {
                    foreach (var order in orders)
                    {
                        if (await client.CancelOrder(order.OrderId))
                        {
                            var updateOrderParams = new
                            {
                                OrderId = order.Id,
                                Status = order.Status
                            };

                            var exec = await dbConnection.ExecuteAsync(
                                "UPDATE BotOrders SET `Status` = @Status WHERE `Id` = @Id",
                                updateOrderParams);

                            if (exec != 1)
                                Log.Error("UzxMarketMarkerJob:CancelOrder {@data}", updateOrderParams);
                        }
                    }
                }

                #endregion

                #region Exchange Data

                var symbolThumbs = await client.GetSymbolThumb();

                if (symbolThumbs == null || symbolThumbs.Count == 0)
                {
                    Log.Error("UzxMarketMarkerJob symbolThumbs");
                    return;
                }

                var symbol = $"{bot.Base}/{bot.Quote}";

                var symbolThumb = symbolThumbs.FirstOrDefault(x => x.Symbol == symbol);
                if (symbolThumb == null)
                {
                    Log.Error("UzxMarketMarkerJob pairThumb");
                    return;
                }

                var lastPrice = symbolThumb.LastPrice;
                if (lastPrice <= 0)
                {
                    Log.Error("UzxMarketMarkerJob pairLastPrice");
                    return;
                }

                var btcThumb = symbolThumbs.FirstOrDefault(x => x.Symbol == "BTC/USDT");
                if (btcThumb == null)
                {
                    Log.Error("UzxMarketMarkerJob btcThumb");
                    return;
                }

                var lastBtcPrice = btcThumb.LastPrice;
                if (lastBtcPrice <= 0)
                {
                    Log.Error("UzxMarketMarkerJob lastBtcPrice");
                    return;
                }

                var makerOption = JsonConvert.DeserializeObject<BotMakerOption>(bot.MakerOption);

                bot.NextRunMakerTime =
                    now + (int)RandomNumber(makerOption.MinInterval, makerOption.MaxInterval, 0) * 1000;

                if (makerOption.FollowBtcBasePrice <= 0)
                {
                    Log.Error("UzxMarketMarkerJob FollowBtcBasePrice");
                    return;
                }

                if (makerOption.FollowBtcBtcPrice <= 0)
                {
                    Log.Error("UzxMarketMarkerJob FollowBtcBtcPrice");
                    return;
                }

                #endregion

                #region Stop Condition

                if (makerOption.MinStopPrice < 0 && lastPrice <= makerOption.MinStopPrice)
                    bot.Status = BotStatus.INACTIVE;

                if (makerOption.MaxStopPrice > 0 && lastPrice >= makerOption.MaxStopPrice)
                    bot.Status = BotStatus.INACTIVE;

                #endregion

                #region Update Bot

                await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET `Status` = @Status, `NextRunMakerTime` = @NextRunMakerTime WHERE Id = @Id",
                    bot);

                #endregion

                var numOfTrades =
                    (int)RandomNumber(makerOption.MinTradePerExec, makerOption.MaxTradePerExec, 0);

                var basePrecision = symbolThumb.BasePrecision;
                var quotePrecision = symbolThumb.QuotePrecision;

                var tasks = new List<Task>();
                for (var i = 0; i < numOfTrades; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        #region Price

                        var change = 100 * (lastBtcPrice - makerOption.FollowBtcBtcPrice) /
                                     makerOption.FollowBtcBtcPrice;

                        if (makerOption.FollowBtcRate > 0)
                            change *= makerOption.FollowBtcRate;

                        var price = RandomNumber(
                            makerOption.FollowBtcBasePrice + makerOption.FollowBtcBasePrice *
                            (change + makerOption.MinPriceStep) / 100,
                            makerOption.FollowBtcBasePrice + makerOption.FollowBtcBasePrice *
                            (change + makerOption.MaxPriceStep) / 100, quotePrecision);

                        if (price <= 0)
                        {
                            Log.Warning("BOT {0} price=0", bot.Symbol);
                            return;
                        }

                        #endregion

                        #region Qty

                        decimal qty;

                        if (makerOption.IsRandomQty)
                        {
                            qty = RandomNumber(makerOption.MinQty, makerOption.MinQty * 2, basePrecision);
                        }
                        else
                        {
                            qty = RandomNumber(makerOption.MinQty, makerOption.MaxQty, basePrecision);
                        }

                        #endregion

                        #region Trade

                        price = price.Truncate(quotePrecision);
                        qty = qty.Truncate(basePrecision);

                        if (makerOption.Side == OrderSide.BUY)
                        {
                            await CreateLimitOrder(client, bot,
                                qty,
                                price,
                                OrderSide.BUY);
                        }
                        else if (makerOption.Side == OrderSide.SELL)
                        {
                            await CreateLimitOrder(client, bot,
                                qty,
                                price,
                                OrderSide.SELL);
                        }
                        else if (makerOption.Side == OrderSide.BOTH)
                        {
                            if (makerOption.MinMatchingTime == 0 && makerOption.MaxMatchingTime == 0)
                            {
                                if (await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL))
                                {
                                    await CreateLimitOrder(client, bot,
                                        qty,
                                        price,
                                        OrderSide.BUY);
                                }
                            }
                            else
                            {
                                if (await CreateLimitOrder(client, bot, qty, price, OrderSide.SELL))
                                {
                                    await TradeDelay(bot);

                                    await CreateLimitOrder(client, bot, qty, price, OrderSide.BUY);
                                }
                            }
                        }

                        #endregion

                        #region Order Over Step

                        if (makerOption.MinPriceOverStep < 0 && price > 0)
                        {
                            var overStepQty = (qty / 2).Truncate(basePrecision);

                            var overStepPrice = RandomNumber(
                                price + (makerOption.MinPriceStep + makerOption.MinPriceOverStep) * price /
                                100,
                                price + makerOption.MinPriceStep * price / 100, quotePrecision);

                            if (overStepPrice > 0)
                                await CreateLimitOrder(client, bot, overStepQty, overStepPrice, OrderSide.BUY, true);
                        }

                        if (makerOption.MaxPriceOverStep > 0 && price > 0)
                        {
                            var overStepQty = (qty / 2).Truncate(basePrecision);

                            var overStepPrice = RandomNumber(
                                price + makerOption.MaxPriceOverStep *
                                price / 100,
                                price + (makerOption.MaxPriceStep + makerOption.MaxPriceOverStep) *
                                price / 100, quotePrecision);

                            if (overStepPrice > 0)
                                await CreateLimitOrder(client, bot, overStepQty, overStepPrice, OrderSide.SELL, true);
                        }

                        #endregion
                    }, CancellationToken.None));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Log.Error(e, "UzxMarketMarkerJob:Run");
            }
        }

        #region Private

        private async Task<bool> CreateLimitOrder(UzxClient client, BotDto bot, decimal qty, decimal price,
            OrderSide side, bool isOverStepOrder = false)
        {
            var uzxOrder = await client.CreateOrder(bot.Base, bot.Quote, qty,
                price, side);

            if (uzxOrder == null)
                return false;

            if (string.IsNullOrEmpty(uzxOrder.OrderId))
                return false;

            var msg = $"{side:G} {qty:F8} {bot.Symbol} at {price:F8} - #{uzxOrder.OrderId}";
            Log.Information("UzxMarketMarker create order {0}", msg);

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var order = new OrderDto
            {
                BotId = bot.Id,
                BotType = bot.Type,
                UserId = bot.UserId,
                OrderId = uzxOrder.OrderId,
                OrderListId = AppUtils.NowMilis(),
                Symbol = bot.Symbol,
                Side = side.ToString("G"),
                Price = price.ToString(new CultureInfo("en-US")),
                OrigQty = qty.ToString(new CultureInfo("en-US")),
                Status = OrderStatus.UNKNOWN,
                Type = "LIMIT_PRICE",
                TransactTime = AppUtils.NowMilis(),
                BotExchangeType = bot.ExchangeType,
            };

            if (isOverStepOrder && bot.MakerOptionObj is { OrderExp: > 0 })
                order.ExpiredTime = order.TransactTime + bot.MakerOptionObj.OrderExp * 1000;
            else
                order.ExpiredTime = order.TransactTime + MexcBotConstants.ExpiredOrderTime;

            var exec = await dbConnection.ExecuteAsync(
                @"INSERT INTO BotOrders(BotId,BotType,BotExchangeType,UserId,OrderId,Symbol,OrderListId,Price,OrigQty,`Type`,Side,ExpiredTime,Status,`TransactTime`)
                      VALUES(@BotId,@BotType,@BotExchangeType,@UserId,@OrderId,@Symbol,@OrderListId,@Price,@OrigQty,@Type,@Side,@ExpiredTime,@Status,@TransactTime)",
                order);

            if (exec == 0)
                Log.Error("UzxMarketMarker insert order {@data}", order);

            return true;
        }


        private async Task TradeDelay(BotDto bot)
        {
            var option = JsonConvert.DeserializeObject<BotMakerOption>(bot.MakerOption);

            await Task.Delay((int)RandomNumber(option.MinMatchingTime,
                option.MaxMatchingTime, 1) * 1000);
        }

        private decimal RandomNumber(decimal from, decimal to, int precision)
        {
            if (from >= to)
                return from;

            var roundPrecision = (int)Math.Pow(10, precision);

            return (decimal)new Random().Next((int)(from * roundPrecision), (int)(to * roundPrecision)) /
                   roundPrecision;
        }

        #endregion
    }
}