using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using multexbot.Api.Infrastructure.Filters;

namespace multexbot.Api
{
    public partial class Startup
    {
        public void ConfigureMvc(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.Add(typeof(ExceptionFilter));
                    options.Filters.Add(new ValidationFilter());
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
        }

        public void ConfigureMvc(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }
        
        public void ConfigureRouting(IApplicationBuilder app)
        {
            app.UseRouting();
        }
    }
}