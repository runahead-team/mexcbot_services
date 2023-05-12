using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mexcbot.Api.Controllers.Base;
using mexcbot.Api.Infrastructure.Authentication;
using mexcbot.Api.RequestModels.User;
using mexcbot.Api.Services.Interface;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace mexcbot.Api.Controllers
{
    [Route("user")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly RsaJwtTokenProvider _rsaJwtTokenProvider;

        public UserController(IUserService userService,
            RsaJwtTokenProvider rsaJwtTokenProvider)
        {
            _userService = userService;
            _rsaJwtTokenProvider = rsaJwtTokenProvider;
        }
        
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<OkResponse> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.LoginAsync(request);

            if (user == null)
                throw new AppException();

            var appUser = new AppUser()
            {
                Username = user.Username,
                Email = user.Email
            };

            var jwt = _rsaJwtTokenProvider.GenerateToken(appUser);

            return new OkResponse(jwt);
        }
    }
}