using Microsoft.Extensions.DependencyInjection;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.Authentication;
using mexcbot.Api.Jobs;
using mexcbot.Api.Jobs.Custom;
using mexcbot.Api.Jobs.DeepCoin;
using mexcbot.Api.Services;
using mexcbot.Api.Services.Interface;
using mexcbot.Api.Services.SubService;

namespace mexcbot.Api
{
    public partial class Startup
    {
        private void RegisterServices(IServiceCollection services)
        {
            //Token
            services.AddSingleton(new TokenManager(Configurations.HashKey));

            //Jwt
            services.AddSingleton<RsaJwtTokenProvider>();

            //Services
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IBotService, BotService>();

            //Job
            services.AddHostedService<VolMarkerJob>();
            services.AddHostedService<LbankVolMakerJob>();
            services.AddHostedService<MarketMarkerJob>();
            services.AddHostedService<CancelOrderJob>();
            //
            services.AddHostedService<DeepCoinVolMakerJob>();
            services.AddHostedService<DeepCoinMarketMarkerJob>();
            services.AddHostedService<BybitVolMarkerJob>();
            services.AddHostedService<BotMonitorJob>();
        }
    }
}