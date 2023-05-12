using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace mexcbot.Api.Controllers.Base
{
    [Authorize(AuthenticationSchemes = "USER")]
    public class BaseController : Controller
    {
        protected AppUser CurrentUser(bool allowAnonymous = false)
        {
            var claims = HttpContext.User.Claims.ToList();

            var idClaim = claims.FirstOrDefault(x => x.Type == "id");


            if (idClaim == null)
            {
                if (allowAnonymous)
                    return null;
                throw new AppException("id");
            }

            var username = claims.FirstOrDefault(x => x.Type == "username")?.Value ??
                           throw new AppException("username");

            var email = claims.FirstOrDefault(x => x.Type == "email")?.Value ?? throw new AppException("email");
            var scopes = claims.Where(x => x.Type == "scope").Select(x => x.Value).ToList();

            return new AppUser
            {
                Id = long.Parse(idClaim.Value),
                Username = username,
                Email = email,
                Scopes = scopes
            };
        }
    }
}