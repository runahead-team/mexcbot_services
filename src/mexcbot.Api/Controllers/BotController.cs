using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mexcbot.Api.Controllers.Base;
using mexcbot.Api.Infrastructure.Authentication;
using mexcbot.Api.Infrastructure.ExchangeClient;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.RequestModels.User;
using mexcbot.Api.Services.Interface;
using Newtonsoft.Json.Linq;
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

        [HttpPost("list")]
        public async Task<OkResponse> GetList([FromBody] TableRequest request)
        {
            var bots = await _botService.GetBotsAsync(request, CurrentUser());

            return new OkResponse(bots);
        }

        [HttpPost("")]
        public async Task<OkResponse> Create([FromBody] BotDto request)
        {
            var bot = await _botService.CreateAsync(request, CurrentUser());

            return new OkResponse(bot);
        }

        [HttpPut("")]
        public async Task<OkResponse> Update([FromBody] BotDto request)
        {
            await _botService.UpdateAsync(request, CurrentUser());

            return new OkResponse();
        }
        
        [HttpPost("order-history")]
        public async Task<OkResponse> GetOrderHistory([FromBody] TableRequest request)
        {
            var orderHistory = await _botService.GetOrderHistoryAsync(request, CurrentUser());

            return new OkResponse(orderHistory);
        }
    }
}