using Microsoft.Extensions.DependencyInjection;
using multexBot.Api.Infrastructure;
using multexBot.Api.Infrastructure.Authentication;
using multexBot.Api.Jobs;
using multexBot.Api.Services;
using multexBot.Api.Services.Interface;
using multexBot.Api.Services.SubService;
using sp.Core.Exchange;

namespace multexBot.Api
{
    public partial class Startup
    {
        private void RegisterServices(IServiceCollection services)
        {
            //Token
            services.AddSingleton(new TokenManager(Configurations.HashKey));

            //Mail
            services.AddSingleton<Mailer>();

            //Jwt
            services.AddSingleton<RsaJwtTokenProvider>();
            services.AddSingleton<AdmRsaJwtTokenProvider>();

            //Exchanges
            services.AddSingleton<BinanceExchange>();
            services.AddSingleton<HoubiExchange>();
            services.AddSingleton<CoinbaseExchange>();

            //Services
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IBotService, BotService>();

            //Job
            services.AddHostedService<BotJob>();
        }
    }
}