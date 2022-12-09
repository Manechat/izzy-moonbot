using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

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
    [Summary("Get a users pressure")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("user", ParameterType.User, "The user to get the pressure of, or yourself if no user is provided.", true)]
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
        var user = await context.Channel.GetUserAsync(userId);

        if (user == null)
        {
            await context.Channel.SendMessageAsync("Couldn't find that user in this server");
        }
        else
        {
            double pressure = _spamService.GetPressure(user.Id);

            await context.Channel.SendMessageAsync($"Current Pressure for {user.Username}#{user.Discriminator}: {pressure}");
        }
    }

    [Command("getmessages")]
    [Summary("Get a users previous messages (the messages which would have been deleted if the user spammed).")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("user", ParameterType.User, "The user to get the messages of, or yourself if no user is provided.", true)]
    public async Task GetPreviousMessagesAsync(
        [Remainder] string userName = "")
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
            var previousMessages = _spamService.GetPreviousMessages(user.Id);

            var messageList = previousMessages.Select(item => 
                $"https://discord.com/channels/{item.GuildId}/{item.ChannelId}/{item.Id} at <t:{item.Timestamp.ToUniversalTime().ToUnixTimeSeconds()}:F> (<t:{item.Timestamp.ToUniversalTime().ToUnixTimeSeconds()}:R>)"
            );

            await ReplyAsync(
                $"I consider the following messages from {user.Username}#{user.Discriminator} to be recent: {Environment.NewLine}{string.Join(Environment.NewLine, messageList)}{Environment.NewLine}*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*");
        }
    }
}