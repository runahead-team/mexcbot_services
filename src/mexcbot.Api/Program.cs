using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using sp.Core.Utils;
using mexcbot.Api.Infrastructure;
using sp.Core.Constants;

namespace mexcbot.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var env = AppUtils.GetEnv();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{env}.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            configuration.Bind(new Configurations());

            #region Enums

            Configurations.Enums = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(x => !string.IsNullOrEmpty(x.Namespace) && x.Namespace.StartsWith("mexcbot.Api") && x.IsEnum)
                .ToDictionary(x => x.Name, x => Enum.GetValues(x).Cast<object>().ToList());

            Configurations.Enums.Add(nameof(AppError), Enum.GetValues(typeof(AppError)).Cast<object>().ToList());

            #endregion

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();

            try
            {
                Log.Information("MexcBot Running");

                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "MexcBot terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(builder =>
                    builder.UseStartup<Startup>()
                        .UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Any, 6000, o => o.Protocols =
                                HttpProtocols.Http1);
                        })
                );
    }
}