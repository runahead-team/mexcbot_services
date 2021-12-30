using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multexbot.Api.Controllers.Base;
using multexbot.Api.Infrastructure.Authentication;
using multexbot.Api.RequestModels.User;
using multexbot.Api.Services.Interface;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace multexbot.Api.Controllers
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

        #region Register

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<OkResponse> Register([FromQuery] int ver, [FromBody] RegisterRequest request)
        {
            await _userService.Register(request);

            return new OkResponse();
        }

        [HttpPost("register-otp")]
        [AllowAnonymous]
        public async Task<OkResponse> RegisterOtp([FromBody] SendRegisterOtpRequest request)
        {
            await _userService.SendRegisterOtp(request);

            return new OkResponse();
        }

        #endregion

        #region Login

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<OkResponse> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.Login(request);

            if (user == null)
                throw new AppException();

            var appUser = new AppUser()
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            var jwt = _rsaJwtTokenProvider.GenerateToken(appUser);

            return new OkResponse(jwt);
        }

        #endregion

        #region Password

        [HttpGet("forgot-pwd/{username}")]
        [AllowAnonymous]
        public async Task<OkResponse> ForgotPwd(string username)
        {
            await _userService.ForgotPassword(username);

            return new OkResponse();
        }

        [HttpPut("reset-pwd")]
        [AllowAnonymous]
        public async Task<OkResponse> ResetPwd([FromBody] ResetPasswordRequest request)
        {
            await _userService.ResetPassword(request);

            return new OkResponse();
        }

        [HttpPut("change-pwd")]
        public async Task<OkResponse> ChangePwd([FromBody] ChangePasswordRequest request)
        {
            await _userService.ChangePassword(request, CurrentUser(true));

            return new OkResponse();
        }

        #endregion

        #region Profile

        [HttpGet("profile")]
        public async Task<OkResponse> GetProfile()
        {
            var result = await _userService.GetProfile(CurrentUser(true).Id);

            return new OkResponse(result);
        }
        
        [HttpPut("profile")]
        public async Task<OkResponse> UpdateProfile([FromBody] UserUpdateRequest request)
        {
            var result = await _userService.UpdateProfile(request,CurrentUser(true));

            return new OkResponse(result);
        }

        #endregion

        #region Security

        [HttpGet("ga")]
        public async Task<OkResponse> GetGaSetup()
        {
            var result = await _userService.GetGaSetup(CurrentUser(true));

            return new OkResponse(result);
        }

        [HttpPut("ga")]
        public async Task<OkResponse> GetGaSetup([FromBody] GaSetupRequest request)
        {
            await _userService.SetupGa(request, CurrentUser(true));

            return new OkResponse();
        }

        #endregion
    }
}