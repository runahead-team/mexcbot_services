using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.ResponseModels.Order;
using mexcbot.Api.Services.Interface;
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

                builder.Where("UserId = @UserId", new { UserId = appUser.Id });

                var total = await dbConnection.ExecuteScalarAsync<int>(
                    counter.RawSql, counter.Parameters);

                var items = (await dbConnection.QueryAsync<BotDto>(
                    selector.RawSql, selector.Parameters)).ToList();

                result = new PagingResult<BotDto>(items, total, request);

                await Task.CompletedTask;
            });

            return result;
        }

        public async Task<BotDto> CreateAsync(BotUpsertRequest request, AppUser appUser)
        {
            var bot = new BotDto(request, appUser);

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                var exec = await dbConnection.ExecuteAsync(
                    @"INSERT INTO Bots(UserId,Base,Quote,Type,ApiKey,ApiSecret,Logs,Status,VolumeOption,MakerOption,CreatedTime)
                    VALUES(@UserId,@Base,@Quote,@Type,@ApiKey,@ApiSecret,@Logs,@Status,@VolumeOption,@MakerOption,@CreatedTime)",
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

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                if ((await dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Bots WHERE UserId = @UserId AND Base = @Base AND Quote = @Quote",
                        request)) != 1)
                    throw new AppException("Bot is not exist");

                var exec = await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET VolumeOption = @VolumeOption, MakerOption = @MakeMakerOptionrOptions, Status = @Status, Type = @Type
                    WHERE UserId = @UserId AND Base = @Base AND Quote = @Quote",
                    request);

                if (exec != 1)
                    throw new AppException(AppError.OPERATION_FAIL);

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

                var total = await dbConnection.ExecuteScalarAsync<int>(
                    counter.RawSql, counter.Parameters);

                var items = (await dbConnection.QueryAsync<OrderDto>(
                    selector.RawSql, selector.Parameters)).ToList();

                result = new PagingResult<OrderDto>(items, total, request);

                await Task.CompletedTask;
            });

            return result;
        }

        #region Sys

        #endregion
    }
}