using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using mexcbot.Api.Infrastructure;

namespace mexcbot.Api
{
    public partial class Startup
    {
        public void ConfigureAuth(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            
            services
                .AddAuthentication()
                .AddJwtBearer("USER",
                    options =>
                    {
                        options.TokenValidationParameters = GetTokenValidationParameters();
                    })
                .AddJwtBearer("ADMIN",
                    options =>
                    {
                        options.TokenValidationParameters = GetAdminTokenValidationParameters();
                    });

            services.AddAuthorization();
        }

        public void ConfigureAuth(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseAuthorization();
        }
        
        private TokenValidationParameters GetTokenValidationParameters()
        {
            var privateKey = Convert.FromBase64String(Configurations.RsaPrvKey);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);

            return new TokenValidationParameters
            {
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateLifetime = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(5),
                RoleClaimType = "scope"
            };
        }
        
        private TokenValidationParameters GetAdminTokenValidationParameters()
        {
            var privateKey = Convert.FromBase64String(Configurations.AdmRsaPrvKey);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);

            return new TokenValidationParameters
            {
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateLifetime = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(5),
                RoleClaimType = "scope"
            };
        }
    }
}