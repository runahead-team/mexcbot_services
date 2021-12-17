using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using multexBot.Api.Infrastructure;
using multexBot.Api.Models.PriceOption;
using multexBot.Api.RequestModels.PriceOption;
using multexBot.Api.ResponseModels.PriceOption;
using multexBot.Api.Services.Interface;
using MySqlConnector;
using sp.Core.Constants;
using sp.Core.Exceptions;

namespace multexBot.Api.Services
{
    public class PriceOptionService : IPriceOptionService
    {
        public async Task<List<PriceOptionView>> GetList(long botId)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var priceOptions = await dbConnection.QueryAsync<PriceOptionView>(
                "SELECT * FROM PriceOptions WHERE BotId = @BotId AND IsActive = @IsActive", new
                {
                    BotId = botId,
                    IsActive = true
                });

            return priceOptions.ToList();
        }

        public async Task<PriceOptionView> Create(PriceOptionCreateRequest request)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var priceOption = new PriceOptionEntity(request);

            var exec = await dbConnection.ExecuteAsync(
                @"INSERT INTO PriceOptions(`Guid`,BotId,RunTime,EndTime,IsActive)
                    VALUES(@Guid,@BotId,@RunTime,@EndTime,@IsActive)",
                priceOption);

            if (exec == 0)
                throw new AppException(AppError.UNKNOWN, "Insert Price Option fail");

            priceOption.Id = await dbConnection.QueryFirstOrDefaultAsync<int>(
                "SELECT Id FROM PriceOptions WHERE Guid = @Guid", new
                {
                    Guid = priceOption.Guid
                });

            var priceOptionView = new PriceOptionView(priceOption);

            return priceOptionView;
        }

        public async Task Update(PriceOptionUpdateRequest request)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var priceOption = new PriceOptionEntity(request);

            var exec = await dbConnection.ExecuteAsync(
                @"UPDATE PriceOptions SET StartTime = @StartTime, EndTime = @EndTime, IsActive = @IsActive WHERE Id = @Id AND BotId = @BotId",
                priceOption);

            if (exec == 0)
                throw new AppException(AppError.UNKNOWN, "Update Price Option fail");
        }

        #region Sys

        public async Task<List<PriceOptionDto>> SysGetList(long botId)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var priceOptions = await dbConnection.QueryAsync<PriceOptionDto>(
                "SELECT * FROM PriceOptions WHERE BotId = @BotId", new
                {
                    BotId = botId,
                });

            return priceOptions.ToList();
        }        

        #endregion
    }
}