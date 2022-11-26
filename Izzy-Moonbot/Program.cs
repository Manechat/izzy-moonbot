using System;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Izzy_Moonbot;

public class Program
{
    public static void Main(string[] args)
    {
        var loggerConfig = new LoggerConfiguration().Enrich.FromLogContext().WriteTo
            .Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        
        #if DEBUG
            // Verbose debug logging.
            loggerConfig = loggerConfig.MinimumLevel.Verbose();
        #else
            // Normal logging
            loggerConfig = loggerConfig.MinimumLevel.Verbose();
        #endif
            
        Log.Logger = loggerConfig.CreateLogger();

        try
        {
            Log.Information("Starting up");
            #if DEBUG
            CreateHostBuilder(args).UseEnvironment("Development").Build().Run();
            #else
            CreateHostBuilder(args).UseEnvironment("Production").Build().Run();
            #endif
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService().ConfigureAppConfiguration(
            (hostContext, builder) =>
            {
                if (hostContext.HostingEnvironment.IsDevelopment()) builder.AddUserSecrets<Program>();
            }).ConfigureServices((hostContext, services) =>
        {
            // Configuration
            var discordSettings = hostContext.Configuration;
            services.Configure<DiscordSettings>(discordSettings.GetSection(nameof(DiscordSettings)));
            var config = FileHelper.LoadConfigAsync().GetAwaiter().GetResult();
            services.AddSingleton(config);
            var users = FileHelper.LoadUsersAsync().GetAwaiter().GetResult();
            services.AddSingleton(users);
            var scheduledTasks = FileHelper.LoadScheduleAsync().GetAwaiter().GetResult();
            services.AddSingleton(scheduledTasks);
            var generalStorage = FileHelper.LoadGeneralStorageAsync().GetAwaiter().GetResult();
            services.AddSingleton(generalStorage);
            var stateStorage = new State();
            services.AddSingleton(stateStorage);
            var quoteStorage = FileHelper.LoadQuoteStorageAsync().GetAwaiter().GetResult();
            services.AddSingleton(quoteStorage);

            // Describers
            services.AddSingleton<ConfigDescriber>();

            // Services
            services.AddSingleton<LoggingService>();
            services.AddSingleton<ModLoggingService>();
            services.AddSingleton<SpamService>();
            services.AddSingleton<ModService>();
            services.AddSingleton<RaidService>();
            services.AddSingleton<FilterService>();
            services.AddSingleton<ScheduleService>();
            services.AddSingleton<QuoteService>();
            services.AddSingleton(services);
            
            // EventListeners
            services.AddSingleton<ConfigListener>();
            services.AddSingleton<UserListener>();
            
            // Misc
            services.AddTransient<IDateTimeService, DateTimeService>();

            services.AddHostedService<Worker>();
        });
    }
}