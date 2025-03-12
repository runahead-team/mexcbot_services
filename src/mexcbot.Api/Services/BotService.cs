using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Infrastructure.Telegram;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.Services.Interface;
using MySqlConnector;
using Newtonsoft.Json;
using Serilog;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;
using sp.Core.Utils;

namespace mexcbot.Api.Services
{
    public class BotService : IBotService
    {
        public BotService()
        {
        }

        public async Task<PagingResult<BotDto>> GetBotsAsync(TableRequest request, AppUser appUser)
        {
            var result = new PagingResult<BotDto>();

            #region Validation

            #endregion

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var builder = new SqlBuilder();
                var (skip, take) = request.GetSkipTake();

                var counter =
                    builder.AddTemplate(
                        @"SELECT COUNT(*)  FROM Bots /**where**/");

                var selector =
                    builder.AddTemplate(
                        @"SELECT *  FROM Bots /**where**/ LIMIT @Skip, @Take",
                        new
                        {
                            Skip = skip,
                            Take = take
                        });

                builder.Where("UserId = @UserId",
                    new { UserId = appUser.Id });

                var total = await dbConnection.ExecuteScalarAsync<int>(
                    counter.RawSql, counter.Parameters);

                var items = (await dbConnection.QueryAsync<BotDto>(
                    selector.RawSql, selector.Parameters)).ToArray();

                await MapOrderAsync(items);

                result = new PagingResult<BotDto>(items, total, request);

                await Task.CompletedTask;
            });

            return result;
        }

        public async Task<BotDto> GetBot(BotGetRequest request, AppUser appUser)
        {
            var result = new BotDto();

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var bot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                    "SELECT * FROM Bots WHERE Type = @Type AND Id = @Id AND UserId = @UserId",
                    new
                    {
                        Id = request.Id,
                        Type = request.Type,
                        UserId = appUser.Id
                    });

                result = bot ?? throw new AppException("Bot not found");

                await MapOrderAsync(result);

                await Task.CompletedTask;
            });

            return result;
        }

        public async Task<BotDto> CreateAsync(BotUpsertRequest request, AppUser appUser)
        {
            var bot = new BotDto(request, appUser);
            bot.Base = bot.ExchangeType == BotExchangeType.LBANK ? bot.Base.ToLower() : bot.Base;
            bot.Quote = bot.ExchangeType == BotExchangeType.LBANK ? bot.Quote.ToLower() : bot.Quote;

            ExchangeClient client = bot.ExchangeType switch
            {
                BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
                BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey, bot.ApiSecret),
                BotExchangeType.DEEPCOIN => new DeepCoinClient(Configurations.DeepCoinUrl, bot.ApiKey, bot.ApiSecret,
                    bot.Passphrase),
                BotExchangeType.COINSTORE =>
                    new CoinStoreClient(Configurations.CoinStoreUrl, bot.ApiKey, bot.ApiSecret),
                _ => null
            };

            if (client != null)
            {
                var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
                var accBalances = await client.GetAccInformation();

                var accInfo = new AccInfo
                {
                    Balances = accBalances
                };

                bot.ExchangeInfo = (exchangeInfo == null || string.IsNullOrEmpty(exchangeInfo.Symbol))
                    ? string.Empty
                    : JsonConvert.SerializeObject(exchangeInfo);
                bot.AccountInfo = (!accBalances.Any())
                    ? string.Empty
                    : JsonConvert.SerializeObject(accInfo);
            }

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var exec = await dbConnection.ExecuteAsync(
                    @"INSERT INTO Bots(UserId,Base,Quote,Type,ExchangeType,ApiKey,ApiSecret,Passphrase,Logs,Status,VolumeOption,MakerOption,ExchangeInfo,AccountInfo,CreatedTime)
                    VALUES(@UserId,@Base,@Quote,@Type,@ExchangeType,@ApiKey,@ApiSecret,@Passphrase,@Logs,@Status,@VolumeOption,@MakerOption,@ExchangeInfo,@AccountInfo,@CreatedTime)",
                    bot);

                if (exec != 1)
                    throw new AppException(AppError.OPERATION_FAIL);

                await Task.CompletedTask;
            });

            return bot;
        }

        public async Task UpdateAsync(BotUpsertRequest request, AppUser appUser)
        {
            var bot = new BotDto(request, appUser);
            bot.Base = bot.ExchangeType == BotExchangeType.LBANK ? bot.Base.ToLower() : bot.Base;
            bot.Quote = bot.ExchangeType == BotExchangeType.LBANK ? bot.Quote.ToLower() : bot.Quote;

            // ExchangeClient client = bot.ExchangeType switch
            // {
            //     BotExchangeType.MEXC => new MexcClient(Configurations.MexcUrl, bot.ApiKey, bot.ApiSecret),
            //     BotExchangeType.LBANK => new LBankClient(Configurations.LBankUrl, bot.ApiKey, bot.ApiSecret),
            //     BotExchangeType.DEEPCOIN => new DeepCoinClient(Configurations.DeepCoinUrl, bot.ApiKey, bot.ApiSecret, bot.Passphrase),
            //     _ => null
            // };

            // if (client != null)
            // {
            //     var exchangeInfo = await client.GetExchangeInfo(bot.Base, bot.Quote);
            //     var accBalances = await client.GetAccInformation();
            //
            //     var accInfo = new AccInfo
            //     {
            //         Balances = accBalances
            //     };
            //
            //     bot.ExchangeInfo = (exchangeInfo == null || string.IsNullOrEmpty(exchangeInfo.Symbol))
            //         ? string.Empty
            //         : JsonConvert.SerializeObject(exchangeInfo);
            //     bot.AccountInfo = (!accBalances.Any())
            //         ? string.Empty
            //         : JsonConvert.SerializeObject(accInfo);
            // }

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                if ((await dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Bots WHERE UserId = @UserId AND Id = @Id AND Type = @Type",
                        bot)) != 1)
                    throw new AppException("Bot is not exist");

                var exec = await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET `Base` = @Base, `Quote` = @Quote, VolumeOption = @VolumeOption, MakerOption = @MakerOption, NextRunMakerTime = 0
                    WHERE UserId = @UserId AND Id = @Id AND Type = @Type",
                    bot);

                if (exec != 1)
                    throw new AppException(AppError.OPERATION_FAIL);

                await Task.CompletedTask;
            });
        }

        public async Task UpdateStatusAsync(BotUpdateStatusRequest request, AppUser appUser)
        {
            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var bot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                    "SELECT * FROM Bots WHERE UserId = @UserId AND Id = @Id",
                    new
                    {
                        UserId = appUser.Id,
                        Id = request.Id
                    });

                if (bot == null)
                    throw new AppException("Bot is not exist");

                var logs = bot.Logs;

                if (request.Status == BotStatus.INACTIVE && bot.Status == BotStatus.ACTIVE)
                    logs = string.Empty;

                var exec = await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET Status = @Status, Logs = @Logs
                    WHERE UserId = @UserId AND Id = @Id",
                    new
                    {
                        UserId = appUser.Id,
                        Id = request.Id,
                        Status = request.Status,
                        Logs = logs
                    });

                if (exec != 1)
                    throw new AppException(AppError.OPERATION_FAIL);

                if (bot.Status != BotStatus.ACTIVE
                    && request.Status == BotStatus.ACTIVE)
                    Telegram.Send($"🟢 BOT {bot.Base} active");

                if (request.Status == BotStatus.INACTIVE)
                    MemCache.RemoveActiveBot(bot);

                await Task.CompletedTask;
            });
        }

        public async Task<PagingResult<OrderDto>> GetOrderHistoryAsync(TableRequest request, AppUser appUser)
        {
            var result = new PagingResult<OrderDto>();

            #region Validation

            #endregion

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var builder = new SqlBuilder();
                var (skip, take) = request.GetSkipTake();

                var counter =
                    builder.AddTemplate(
                        @"SELECT COUNT(*)  FROM BotOrders /**where**/");

                var selector =
                    builder.AddTemplate(
                        @"SELECT *  FROM BotOrders /**where**/ ORDER BY TransactTime DESC LIMIT @Skip, @Take",
                        new
                        {
                            Skip = skip,
                            Take = take
                        });

                builder.Where("UserId = @UserId", new { UserId = appUser.Id });

                if (request.Filters.TryGetValue("BotId", out var stringBotId))
                {
                    if (long.TryParse(stringBotId, out var botId) && botId > 0)
                        builder.Where("BotId = @BotId", new { BotId = botId });
                }

                if (request.Filters.TryGetValue("BotType", out var botTypeStr))
                {
                    if (Enum.TryParse(botTypeStr, out BotType botType))
                        builder.Where("BotType = @BotType", new { BotType = botType });
                }

                var total = await dbConnection.ExecuteScalarAsync<int>(
                    counter.RawSql, counter.Parameters);

                var items = (await dbConnection.QueryAsync<OrderDto>(
                    selector.RawSql, selector.Parameters)).ToList();

                result = new PagingResult<OrderDto>(items, total, request);

                await Task.CompletedTask;
            });

            return result;
        }

        public async Task DeleteBotAsync(long botId, AppUser appUser)
        {
            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var bot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                    "SELECT * FROM Bots WHERE Id = @Id AND UserId = @UserId",
                    new
                    {
                        Id = botId,
                        UserId = appUser.Id
                    });

                if (bot == null)
                    throw new AppException("Bot not found");

                if (bot.Status == BotStatus.ACTIVE)
                    throw new AppException("Bot is active");

                var deleteParams = new
                {
                    Id = botId,
                    UserId = appUser.Id
                };

                var exec = await dbConnection.ExecuteAsync(
                    "DELETE FROM Bots WHERE Id = @Id AND UserId = @UserId",
                    deleteParams);

                if (exec != 1)
                {
                    Log.Error("BotService:DeleteBotAsync exec fail {@data}", deleteParams);
                    throw new AppException("Delete fail");
                }
            });
        }

        public async Task UpdateBotHistory(BotHistoryDto data)
        {
            try
            {
                var now = AppUtils.NowMilis();
                var d1 = (long)TimeSpan.FromDays(1).TotalMilliseconds;

                data.Date = now - now % d1;
                data.Spread = Math.Abs(data.Spread);

                await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

                var botHistory = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                    "SELECT * FROM BotHistory WHERE BotId = @BotId AND `Date` = @Date",
                    new
                    {
                        BotId = data.BotId,
                        Date = data.Date
                    });


                if (botHistory == null)
                {
                    await dbConnection.ExecuteAsync(
                        @"INSERT INTO BotHistory(BotId,`Date`,BalanceBase,BalanceQuote,Spread)
                            VALUE(@BotId,@Date,@BalanceBase,@BalanceQuote,@Spread)", data);
                }
                else
                {
                    data.Id = botHistory.Id;

                    await dbConnection.ExecuteAsync(
                        @"UPDATE BotHistory SET BalanceBase = @BalanceBase, 
                                BalanceQuote = @BalanceQuote,
                                Spread = @Spread
                            WHERE Id = @Id", data);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateBotHistory");
            }
        }

        #region Prv

        private async Task MapOrderAsync(params BotDto[] bots)
        {
            foreach (var bot in bots)
            {
                bot.ApiSecret = "**********************";
                bot.Passphrase = "***********";
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}