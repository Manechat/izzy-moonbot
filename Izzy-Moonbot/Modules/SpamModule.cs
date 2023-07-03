using System.Linq;
using System.Threading.Tasks;
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
        if (userName == "") userName = $"<@!{context.User.Id}>";

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userName, context);
        var user = context.Guild?.GetUser(userId);

        if (user == null)
        {
            await context.Channel.SendMessageAsync("Couldn't find that user in this server");
        }
        else
        {
            double pressure = _spamService.GetPressure(user.Id);

            await context.Channel.SendMessageAsync($"Current Pressure for {user.DisplayName} ({user.Username}/{user.Id}): {pressure}");
        }
    }

    [Command("getmessages")]
    [Summary("Get a user's previous messages (the messages which would have been deleted if the user spammed).")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("user", ParameterType.UserResolvable, "The user to get the messages of, or yourself if no user is provided.", true)]
    public async Task GetPreviousMessagesAsync(
        [Remainder] string userName = "")
    {
        await TestableGetPreviousMessagesAsync(
            new SocketCommandContextAdapter(Context),
            userName
        );
    }

    public async Task TestableGetPreviousMessagesAsync(
        IIzzyContext context,
        string userName = "")
    {
        // If no target is specified, target self.
        if (userName == "") userName = $"<@!{context.User.Id}>";

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userName, context);
        var user = context.Guild?.GetUser(userId);

        if (user == null)
        {
            await context.Channel.SendMessageAsync("Couldn't find that user in this server");
        }
        else
        {
            var previousMessages = _spamService.GetPreviousMessages(user.Id);

            var messageList = previousMessages.Select(item => 
                $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.Id} at <t:{item.Timestamp.ToUniversalTime().ToUnixTimeSeconds()}:F> (<t:{item.Timestamp.ToUniversalTime().ToUnixTimeSeconds()}:R>)"
            );

            await context.Channel.SendMessageAsync(
                $"I consider the following messages from {user.DisplayName} ({user.Username}/{user.Id}) to be recent: \n{string.Join('\n', messageList)}\n*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*");
        }
    }
}
