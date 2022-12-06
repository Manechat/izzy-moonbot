using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;

namespace Izzy_Moonbot.Attributes;

// "Developer" only command
// List of "Developers" is in appsettings.json.
public class DevCommandAttribute : PreconditionAttribute
{
    public static bool TestMode = false;

    private readonly DiscordSettings? _settings;

    public DevCommandAttribute()
    {
        if (TestMode) return;

        // Get the config.
        // It has to be done like this because attributes don't get the services and settings.
        var config = new ConfigurationBuilder()
            #if DEBUG
            .AddJsonFile("appsettings.Development.json")
            #else
            .AddJsonFile("appsettings.json")
            #endif
            .Build();

        var section = config.GetSection(nameof(DiscordSettings));
        _settings = section.Get<DiscordSettings>();
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        if (TestMode) return Task.FromResult(PreconditionResult.FromSuccess());

        // Check if the user is in the DevUsers list
        // If they are, return success.
        if (_settings?.DevUsers.Any(userId => context.User.Id == userId) ?? false)
            return Task.FromResult(PreconditionResult.FromSuccess());

        // Else, return failure/error.
        return Task.FromResult(PreconditionResult.FromError(""));
    }
}