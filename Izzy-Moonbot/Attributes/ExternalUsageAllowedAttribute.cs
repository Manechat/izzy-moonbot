using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Izzy_Moonbot.Attributes;

// Allow users to use commands outside of DiscordSettings.DefaultGuild.
public class ExternalUsageAllowed : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        // Just return true since the processing for this flag is done on the command handler.
        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}