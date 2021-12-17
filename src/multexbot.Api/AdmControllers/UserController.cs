using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multexBot.Api.AdmControllers.Base;
using multexBot.Api.Constants;
using multexBot.Api.Services.Interface;
using sp.Core.Models;

namespace multexBot.Api.AdmControllers
{
    [Authorize(Roles = MultexBotAdminRoles.USER_READ)]
    [Route("adm-api/user")]
    public class UserController : BaseAdmController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("get-all")]
        public async Task<OkResponse> GetAll([FromBody] TableRequest request)
        {
            var result = await _userService.AdmGetList(request);

            return new OkResponse(result);
        }

        [Authorize(Roles = MultexBotAdminRoles.USER_FULL)]
        [HttpPut("disable")]
        public async Task<OkResponse> Disable([FromQuery] [Required] long userId)
        {
            await _userService.AdmDisable(userId, CurrentUser());

            return new OkResponse();
        }

        [Authorize(Roles = MultexBotAdminRoles.USER_FULL)]
        [HttpPut("active")]
        public async Task<OkResponse> Active([FromQuery] [Required] long userId)
        {
            await _userService.AdmActive(userId, CurrentUser());

            return new OkResponse();
        }

        [Authorize(Roles = MultexBotAdminRoles.USER_FULL)]
        [HttpPut("disable-ga")]
        public async Task<OkResponse> DisableGa([FromQuery] [Required] long userId)
        {
            await _userService.AdmDisableGa(userId, CurrentUser());

            return new OkResponse();
        }
    }
}