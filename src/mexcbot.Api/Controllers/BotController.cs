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
    [Route("bot")]
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
        
        [HttpGet("Candlestick")]
        [AllowAnonymous]
        public async Task<OkResponse> Candlestick()
        {
            var mexcClient = new MexcClient("https://api.mexc.com", "mx0vglSajT0Oz8xiow", "4088b3f33029404cbe071bb84579dd6a");

            var sticks = await mexcClient.GetExchangeInfo("OPV", "USDT");

            return new OkResponse(sticks);
        }
    }
}