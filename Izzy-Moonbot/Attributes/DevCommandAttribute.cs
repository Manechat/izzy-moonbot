using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;

namespace Izzy_Moonbot.Attributes
{
    public class DevCommandAttribute : PreconditionAttribute
    {
        private readonly DiscordSettings _settings;

        public DevCommandAttribute()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var section = config.GetSection(nameof(DiscordSettings));
            _settings = section.Get<DiscordSettings>();
        }
        
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (_settings.DevUsers.Any(userId => gUser.Id == userId))
                    return Task.FromResult(PreconditionResult.FromSuccess());
                
                return Task.FromResult(PreconditionResult.FromError(""));
            }
            return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
        }
    }
}