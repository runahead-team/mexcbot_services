using Microsoft.Extensions.DependencyInjection;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Infrastructure.Authentication;
using mexcbot.Api.Jobs;
using mexcbot.Api.Services;
using mexcbot.Api.Services.Interface;
using mexcbot.Api.Services.SubService;
using sp.Core.Exchange;

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
            services.AddHostedService<BotVolPlaceOrderJob>();
            services.AddHostedService<LbankBotVolPlaceOrderJob>();
            services.AddHostedService<BotMakerPlaceOrderJob>();
            services.AddHostedService<BotCancelOrderJob>();
        }
    }
}