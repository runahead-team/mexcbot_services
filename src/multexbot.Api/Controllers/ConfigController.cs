#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multexBot.Api.Constants;
using multexBot.Api.Controllers.Base;
using multexBot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Models;

namespace multexBot.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/config")]
    public class ConfigController : BaseController
    {
        [HttpGet]
        public OkResponse Get()
        {
            return new OkResponse(new
            {
                enums = Configurations.Enums,
                AppConstants.DateFormat,
                AppConstants.DateTimeFormat,
                
            });
        }
    }
}