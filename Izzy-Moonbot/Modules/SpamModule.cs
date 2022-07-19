using System.Linq;
using Izzy_Moonbot.Attributes;

namespace Izzy_Moonbot.Modules
{
    using Discord;
    using Discord.Commands;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using System.Threading.Tasks;

    [Summary("Module for interacting with the spam prevention services.")]
    public class SpamModule : ModuleBase<SocketCommandContext>
    {
        private ServerSettings _settings;
        private readonly PressureService _pressureService;

        public SpamModule(ServerSettings settings, PressureService pressureService)
        {
            _settings = settings;
            _pressureService = pressureService;
        }

        [Command("getpressure")]
        [Summary("Get a users pressure")]
        [ModCommand(Group = "Permissions")]
        [DevCommand(Group = "Permissions")]
        public async Task GetPressureAsync([Summary("userid")] string userName = "", [Summary("Whether to return the value the users last message returned")][Remainder] string noModifying = "")
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
                double pressure = -1;
                if ("yes|true|y".Split("|").Contains(noModifying))
                {
                    pressure = await _pressureService.GetPressureWithoutModifying(userId);
                }
                else
                {
                    pressure = await _pressureService.GetPressure(userId);
                }
                
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
