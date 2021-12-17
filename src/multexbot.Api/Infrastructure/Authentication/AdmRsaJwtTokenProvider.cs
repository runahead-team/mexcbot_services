using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using multexBot.Api.Constants;
using multexBot.Api.Models.Admin;

namespace multexBot.Api.Infrastructure.Authentication
{
    public class AdmRsaJwtTokenProvider
    {
        private readonly SigningCredentials _signingCredentials;

        public AdmRsaJwtTokenProvider()
        {
            var prvKey = Convert.FromBase64String(Configurations.AdmRsaPrvKey);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(prvKey, out _);

            _signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory {CacheSignatureProviders = false}
            };
        }

        public JwtResponse GenerateToken(AdminDto admin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
        
            var identity = new ClaimsIdentity();
        
            var idClaim = new Claim("admId", admin.Id.ToString());
            identity.AddClaim(idClaim);
        
            var mailClaim = new Claim("email", admin.Email);
            identity.AddClaim(mailClaim);
        
            foreach (var scopeClaim in admin.Scopes.Select(scope => new Claim("scope", scope)))
            {
                identity.AddClaim(scopeClaim);
            }
        
            var willExpire = DateTime.UtcNow.AddSeconds(MultexBotConstants.AdmTokenExpiryInSeconds);
        
            SecurityToken token = tokenHandler.CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                SigningCredentials = _signingCredentials,
                Expires = willExpire,
                Subject = identity
            });
        
            return new JwtResponse
            {
                AccessToken = tokenHandler.WriteToken(token),
                ExpInSeconds = MultexBotConstants.AdmTokenExpiryInSeconds
            };
        }
    }
}