using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Module for interacting with the spam prevention services.")]
public class SpamModule : ModuleBase<SocketCommandContext>
{
    private readonly SpamService _spamService;

    public SpamModule(SpamService spamService)
    {
        _spamService = spamService;
    }

    [Command("getpressure")]
    [Summary("Get a users pressure")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task GetPressureAsync([Remainder][Summary("userid")] string userName = "")
    {
        // If no target is specified, target self.
        if (userName == "") userName = $"<@!{Context.User.Id}>";

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
        var user = await Context.Channel.GetUserAsync(userId);

        if (user == null)
        {
            await ReplyAsync("Couldn't find that user in this server");
        }
        else
        {
            double pressure = _spamService.GetPressure(user.Id);

            await ReplyAsync($"Current Pressure for {user.Username}#{user.Discriminator}: {pressure}");
        }
    }
}