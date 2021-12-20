using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sp.Core.Exceptions;
using sp.Core.Models;

namespace multexbot.Api.AdmControllers.Base
{
    [Authorize(AuthenticationSchemes = "ADMIN")]
    public class BaseAdmController : Controller
    {
        protected AppUser CurrentUser()
        {
            var claims = HttpContext.User.Claims.ToList();

            var idClaim = claims.FirstOrDefault(x => x.Type == "admId");

            if (idClaim == null)
                throw new AppException("id");
            
            var email = claims.FirstOrDefault(x => x.Type == "email")?.Value ??  throw new AppException("email");
            var scopes = claims.Where(x => x.Type == "scope").Select(x=>x.Value).ToList();

            return new AppUser
            {
                Id = long.Parse(idClaim.Value),
                Email = email,
                Scopes = scopes
            };
        }
    }
}