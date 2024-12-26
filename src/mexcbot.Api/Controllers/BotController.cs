using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using mexcbot.Api.Controllers.Base;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace mexcbot.Api.Controllers
{
    [Route("api/bot")]
    public class BotController : BaseController
    {
        private readonly IBotService _botService;

        public BotController(IBotService botService)
        {
            _botService = botService;
        }

        [AllowAnonymous]
        [HttpPost("orders")]
        public async Task<OkResponse> GetOrders()
        {
            var client = new LBankClient(Configurations.LBankUrl, "417de6f4-a22d-4ce1-a327-dad4fc4f1161",
                "645B3F229FC65220FE31926BDF94A0EB");

            var orders = await client.GetOpenOrder("lbk", "usdt");

            return new OkResponse(orders);
        }

        [HttpPost("list")]
        public async Task<OkResponse> GetList([FromBody] TableRequest request)
        {
            var bots = await _botService.GetBotsAsync(request, CurrentUser());

            return new OkResponse(bots);
        }

        [HttpGet("single")]
        public async Task<OkResponse> GetBot([FromQuery] BotGetRequest request)
        {
            var bot = await _botService.GetBot(request, CurrentUser());

            return new OkResponse(bot);
        }

        [HttpPost("")]
        public async Task<OkResponse> Create([FromBody] BotUpsertRequest request)
        {
            var bot = await _botService.CreateAsync(request, CurrentUser());

            return new OkResponse(bot);
        }

        [HttpPut("")]
        public async Task<OkResponse> Update([FromBody] BotUpsertRequest request)
        {
            await _botService.UpdateAsync(request, CurrentUser());

            return new OkResponse();
        }

        [HttpPut("status")]
        public async Task<OkResponse> UpdateStatusAsync([FromBody] BotUpdateStatusRequest request)
        {
            await _botService.UpdateStatusAsync(request, CurrentUser());

            return new OkResponse();
        }

        [HttpPost("order-history")]
        public async Task<OkResponse> GetOrderHistory([FromBody] TableRequest request)
        {
            var orderHistory = await _botService.GetOrderHistoryAsync(request, CurrentUser());

            return new OkResponse(orderHistory);
        }

        [HttpDelete("")]
        public async Task<OkResponse> DeleteBotAsync([FromQuery] long id)
        {
            await _botService.DeleteBotAsync(id, CurrentUser());

            return new OkResponse();
        }

        [HttpGet("bot-history")]
        public async Task<OkResponse> GetBotHistory([Required] long id)
        {
            var appUser = CurrentUser();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var bot = await dbConnection.QueryFirstOrDefaultAsync<BotDto>(
                "SELECT * FROM Bots WHERE Id = @Id AND UserId = @UserId",
                new
                {
                    Id = id,
                    UserId = appUser.Id
                });

            if (bot == null)
                throw new AppException();

            var history = await dbConnection.QueryAsync<BotHistoryDto>(
                "SELECT * FROM BotHistory WHERE BotId = @BotId ORDER BY Id DESC",
                new
                {
                    BotId = bot.Id
                });

            return new OkResponse(history);
        }
    }
}