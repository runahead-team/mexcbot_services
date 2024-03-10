using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using mexcbot.Api.Constants;
using sp.Core.Models;
using System.Linq;

namespace mexcbot.Api.Infrastructure.Authentication
{
    public class RsaJwtTokenProvider
    {
        private readonly SigningCredentials _signingCredentials;

        public RsaJwtTokenProvider()
        {
            var prvKey = Convert.FromBase64String(Configurations.RsaPrvKey);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(prvKey, out _);

            _signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory {CacheSignatureProviders = false}
            };
        }

        public JwtResponse GenerateToken(AppUser user, double expiredInSeconds = 0)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var identity = new ClaimsIdentity();

            var idClaim = new Claim("id", user.Id.ToString());
            identity.AddClaim(idClaim);

            var usernameClaim = new Claim("username", user.Username);
            identity.AddClaim(usernameClaim);

            var mailClaim = new Claim("email", user.Email);
            identity.AddClaim(mailClaim);

            if (user.Scopes != null && user.Scopes.Any())
            {
                foreach (var scopeClaim in user.Scopes.Select(scope => new Claim("scope", scope)))
                {
                    identity.AddClaim(scopeClaim);
                }
            }
            var willExpire =
                DateTime.UtcNow.AddSeconds(expiredInSeconds == 0
                    ? MexcBotConstants.TokenExpiryInSeconds
                    : expiredInSeconds);

            SecurityToken token = tokenHandler.CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                SigningCredentials = _signingCredentials,
                Expires = willExpire,
                Subject = identity
            });

            return new JwtResponse
            {
                AccessToken = tokenHandler.WriteToken(token),
                ExpInSeconds = MexcBotConstants.TokenExpiryInSeconds
            };
        }
    }
}