using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Izzy_Moonbot.Attributes;

// Allow bots to use commands.
// This is so we can say "bots can use the roll command but not the ban command"
public class BotsAllowedAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        // Just return true since the processing for this flag is done on the command handler.
        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}