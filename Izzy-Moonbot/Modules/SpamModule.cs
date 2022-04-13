using Discord;
using Discord.Commands;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Modules
{
    [Summary("Module for preventing spam")]
    public class SpamModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly PressureService _pressureService;
        private readonly ServerSettings _settings;
        private readonly Dictionary<ulong, User> _users;

        public SpamModule(LoggingService logger, PressureService pressureService, ServerSettings settings, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _pressureService = pressureService;
            _settings = settings;
            _users = users;
        }
        [Command("pressure")]
        [Summary("get user pressure")]
        public async Task PressureAsync([Summary("userid")][Remainder] string userName = "")
        {
            // If no target is specified, target self.
            if (userName == "") userName = $"<@!{Context.User.Id}>";

            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            var user = await Context.Channel.GetUserAsync(userId);

            if (user == null)
            {
                await ReplyAsync($"Couldn't find that user in this server", allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
            }
            else
            {
                var pressure = await _pressureService.GetPressure(userId);
                if (pressure < 0)
                {
                    await ReplyAsync($"Couldn't find pressure for {user.Username}#{user.Discriminator}. Maybe they haven't spoken before?", allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
                }
                else
                {
                    await ReplyAsync($"Current Pressure for {user.Username}#{user.Discriminator}: {pressure}", allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
                }
            }
        }
    }
}
