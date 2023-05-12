#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mexcbot.Api.Constants;
using mexcbot.Api.Controllers.Base;
using mexcbot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Models;

namespace mexcbot.Api.Controllers
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