using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Models.Bot;
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

        public async Task<BotDto> CreateAsync(BotDto request, AppUser appUser)
        {
            request.UserId = appUser.Id;

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                if ((await dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Bots WHERE Base = @Base AND Quote = @Quote", request)) != 0)
                    throw new AppException("Bot is existed");

                request.CreatedTime = AppUtils.NowMilis();

                var exec = await dbConnection.ExecuteAsync(
                    @"INSERT INTO Bots(UserId,Base,Quote,Volume24hr,MatchingDelayFrom,MatchingDelayTo,ApiKey,ApiSecret,Logs,Status,MinOrderQty,MaxOrderQty,CreatedTime)
                    VALUES(@UserId,@Base,@Quote,@Volume24hr,@MatchingDelayFrom,@MatchingDelayTo,@ApiKey,@ApiSecret,@Logs,@Status,@MinOrderQty,@MaxOrderQty,@CreatedTime)",
                    request);

                if (exec != 1)
                    throw new AppException(AppError.OPERATION_FAIL);

                await Task.CompletedTask;
            });

            return request;
        }

        public async Task UpdateAsync(BotDto request, AppUser appUser)
        {
            request.UserId = appUser.Id;

            await DbConnections.ExecAsync(async (dbConnection) =>
            {
                if ((await dbConnection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Bots WHERE UserId = @UserId AND Base = @Base AND Quote = @Quote",
                        request)) != 1)
                    throw new AppException("Bot is not exist");

                var exec = await dbConnection.ExecuteAsync(
                    @"UPDATE Bots SET Volume24hr = @Volume24hr, MatchingDelayFrom = @MatchingDelayFrom, MatchingDelayTo = @MatchingDelayTo, MinOrderQty = @MinOrderQty, MaxOrderQty = @MaxOrderQty, Status = @Status
                    WHERE UserId = @UserId AND Base = @Base AND Quote = @Quote AND Id = @Id",
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
                        @"SELECT *  FROM BotOrders /**where**/ LIMIT @Skip, @Take",
                        new
                        {
                            Skip = skip,
                            Take = take
                        });

                builder.Where("UserId = @UserId", new { UserId = appUser.Id });

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