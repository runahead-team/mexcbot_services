using Microsoft.Extensions.DependencyInjection;
using multexbot.Api.Infrastructure;
using multexbot.Api.Infrastructure.Authentication;
using multexbot.Api.Jobs;
using multexbot.Api.Services;
using multexbot.Api.Services.Interface;
using multexbot.Api.Services.SubService;
using sp.Core.Exchange;

namespace multexbot.Api
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
            services.AddSingleton<IMarketService, MarketService>();

            //Job
            services.AddHostedService<BotJob>();
            services.AddHostedService<UpdatePriceJob>();
        }
    }
}