using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Izzy_Moonbot.Attributes;

// Allow users to use commands outside of DiscordSettings.DefaultGuild.
// This is so we can allow users to use certain commands outside of DiscordSettings.DefaultGuild,
// An example is .quote or .remindme.
public class DMsAllowedAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        // Just return true since the processing for this flag is done on the command handler.
        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}