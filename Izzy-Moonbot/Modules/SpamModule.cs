using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;

namespace Izzy_Moonbot.Modules;

[Summary("Anti-spam related commands.")]
public class SpamModule : ModuleBase<SocketCommandContext>
{
    private readonly SpamService _spamService;

    public SpamModule(SpamService spamService)
    {
        _spamService = spamService;
    }

    [Command("getpressure")]
    [Summary("Get a user's pressure")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("user", ParameterType.UserResolvable, "The user to get the pressure of, or yourself if no user is provided.", true)]
    public async Task GetPressureAsync([Remainder] string userName = "")
    {
        await TestableGetPressureAsync(
            new SocketCommandContextAdapter(Context),
            userName
        );
    }

    public async Task TestableGetPressureAsync(
        IIzzyContext context,
        string userName = "")
    {
        // If no target is specified, target self.
        if (userName == "") userName = $"<@{context.User.Id}>";

        var (userId, userError) = await ParseHelper.TryParseUserResolvable(userName, context.Guild!);
        if (userId == null)
        {
            await context.Channel.SendMessageAsync($"Failed to get user id from \"{userName}\": {userError}");
            return;
        }

        var user = context.Guild?.GetUser((ulong)userId);
        if (user == null)
        {
            await context.Channel.SendMessageAsync($"Couldn't find <@{userId}> in this server", allowedMentions: AllowedMentions.None);
        }
        else
        {
            double pressure = _spamService.GetPressure(user.Id);

            await context.Channel.SendMessageAsync($"Current Pressure for {user.DisplayName} ({user.Username}/{user.Id}): {pressure}");
        }
    }
}
