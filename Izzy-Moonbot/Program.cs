namespace Izzy_Moonbot
{
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo
                .Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService().ConfigureAppConfiguration((hostContext, builder) =>
            {
                if (hostContext.HostingEnvironment.IsDevelopment())
                {
                    builder.AddUserSecrets<Program>();
                }
            }).ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                services.Configure<DiscordSettings>(config.GetSection(nameof(DiscordSettings)));
                services.AddTransient<IDateTimeService, DateTimeService>();
                services.AddSingleton<LoggingService>();
                var settings = FileHelper.LoadAllPresettingsAsync().GetAwaiter().GetResult();
                services.AddSingleton(settings);
                services.AddSingleton(services);

                services.AddHostedService<Worker>();
            });
    }
}
