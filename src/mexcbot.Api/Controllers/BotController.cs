using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mexcbot.Api.Controllers.Base;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.Services.Interface;
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
    }
}