using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Attributes;

// Moderation only commands
// Used for sensitive moderation commands
public class ModCommandAttribute : PreconditionAttribute
{
    private readonly Config _config = FileHelper.LoadConfigAsync().Result;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        // Check if user originates from guild
        if (context.User is SocketGuildUser gUser)
        {
            // If this command was executed by a user with the appropriate role, return a success
            if (gUser.Roles.Any(r => r.Id == _config.ModRole))
                // Since no async work is done, the result has to be wrapped with `Task.FromResult` to avoid compiler errors
                return Task.FromResult(PreconditionResult.FromSuccess());
            // Since it wasn't, fail
            return Task.FromResult(PreconditionResult.FromError(""));
        }

        // Fail due to the user not originating from a guild (dm or group chat)
        return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
    }
}