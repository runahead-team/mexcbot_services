#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multexbot.Api.Constants;
using multexbot.Api.Controllers.Base;
using multexbot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Models;

namespace multexbot.Api.Controllers
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