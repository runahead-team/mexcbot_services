using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using multexBot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Utils;

namespace multexBot.Api
{
    public partial class Startup
    {
        public void ConfigureCors(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                if (AppUtils.GetEnv() != AppEnvironments.Production)
                {
                    options.AddDefaultPolicy(
                        builder =>
                        {
                            builder
                                .SetIsOriginAllowed(x => AppUtils.GetEnv() != AppEnvironments.Production)
                                .WithOrigins(Configurations.AllowOrigins)
                                .AllowCredentials()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                }
                else
                {
                    options.AddDefaultPolicy(
                        builder =>
                        {
                            builder
                                .WithOrigins(Configurations.AllowOrigins)
                                .AllowCredentials()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                }
                
            });
        }

        public void ConfigureCors(IApplicationBuilder app)
        {
            app.UseCors();
        }
    }
}