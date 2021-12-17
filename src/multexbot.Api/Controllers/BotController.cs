using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using multexBot.Api.Constants;
using multexBot.Api.Controllers.Base;
using multexBot.Api.Models.Bot;
using multexBot.Api.Services.Interface;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace multexBot.Api.Controllers
{
    [Route("bot")]
    public class BotController : BaseController
    {
        private readonly IBotService _botService;

        public BotController(IBotService botService)
        {
            _botService = botService;
        }

        [HttpGet("list")]
        public async Task<OkResponse> GetList([FromQuery] ExchangeType exchangeType)
        {
            var result = await _botService.GetList(exchangeType, CurrentUser(true));

            return new OkResponse(result);
        }

        [HttpPost]
        public async Task<OkResponse> Create([FromBody] BotUpsertRequest request)
        {
            var result = await _botService.Create(request, CurrentUser(true));

            return new OkResponse(result);
        }

        [HttpPut]
        public async Task<OkResponse> Update([FromBody] BotUpsertRequest request)
        {
            await _botService.Update(request, CurrentUser(true));

            return new OkResponse();
        }

        [HttpDelete]
        public async Task<OkResponse> Update(long id)
        {
            if (id <= 0)
                throw new AppException(AppError.INVALID_PARAMETERS, "Missing field id");

            await _botService.Delete(id, CurrentUser(true));

            return new OkResponse();
        }
    }
}