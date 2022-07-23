using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Izzy_Moonbot.Attributes
{
    // Inherit from PreconditionAttribute
    public class BotsAllowedAttribute : PreconditionAttribute
    {
        // Create a constructor so the name can be specified
        public BotsAllowedAttribute() {}
        
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}