using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using multexbot.Api.Infrastructure;
using MySqlConnector.Logging;
using Serilog;
using sp.Core.Utils;

namespace multexbot.Api
{
    public partial class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureCors(services);
            
            ConfigureAuth(services);
            
            ConfigureJson();
            
            RegisterServices(services);

            ConfigureMvc(services);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            ConfigureCors(app);
            
            ConfigureRouting(app);
            
            ConfigureAuth(app);
            
            ConfigureMvc(app);
            
        }
    }
}
