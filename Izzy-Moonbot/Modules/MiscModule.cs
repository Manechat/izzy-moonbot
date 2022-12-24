using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Modules;

[Summary("Misc commands which exist for fun.")]
public class MiscModule : ModuleBase<SocketCommandContext>
{
    private readonly Config _config;
    private readonly ScheduleService _schedule;
    private readonly LoggingService _logger;

    public MiscModule(Config config, ScheduleService schedule, LoggingService logger)
    {
        _config = config;
        _schedule = schedule;
        _logger = logger;
    }

    [Command("banner")]
    [Summary("Get the current banner of Manechat.")]
    [Alias("getbanner", "currentbanner")]
    public async Task BannerCommandAsync()
    {
        if (_config.BannerMode == ConfigListener.BannerMode.ManebooruFeatured)
        {
            await Context.Channel.SendMessageAsync("I'm currently syncing the banner with the Manebooru featured image");
            await Context.Channel.SendMessageAsync(";featured");
            return;
        }

        if (Context.Guild.BannerUrl == null)
        {
            await Context.Channel.SendMessageAsync("No banner is currently set.");
            return;
        }

        var message = "";
        if (_config.BannerMode == ConfigListener.BannerMode.None)
            message += $"I'm not currently managing the banner, but here's the current server's banner.{Environment.NewLine}";

        message += $"{Context.Guild.BannerUrl}?size=4096";

        await Context.Channel.SendMessageAsync(message);
    }

    [Command("snowflaketime")]
    [Summary("Get the creation date of a Discord resource via its snowflake ID.")]
    [Alias("sft")]
    [Parameter("snowflake", ParameterType.Snowflake, "The snowflake to get the creation date from.")]
    [ExternalUsageAllowed]
    public async Task SnowflakeTimeCommandAsync([Remainder]string snowflakeString = "")
    {
        if (snowflakeString == "")
        {
            await Context.Channel.SendMessageAsync("You need to give me a snowflake to convert!");
            return;
        }
        
        try
        {
            var snowflake = ulong.Parse(snowflakeString);
            var time = SnowflakeUtils.FromSnowflake(snowflake);

            await Context.Channel.SendMessageAsync($"`{snowflake}` -> <t:{time.ToUnixTimeSeconds()}:F> (<t:{time.ToUnixTimeSeconds()}:R>)");
        }
        catch
        {
            await Context.Channel.SendMessageAsync("Sorry, I couldn't convert the snowflake you gave me to an actual snowflake.");
        }
    }

    [Command("remindme")]
    [Summary("Ask Izzy to DM you a message in the future.")]
    [Alias("remind", "dmme")]
    [Parameter("time", ParameterType.DateTime,
        "How long to wait until sending the message, e.g. \"5 days\" or \"2 hours\".")]
    [Parameter("message", ParameterType.String, "The reminder message to DM.")]
    [ExternalUsageAllowed]
    [Example(".remindme 2 hours join stream")]
    [Example(".remindme 6 months rethink life")]
    public async Task RemindMeCommandAsync([Remainder] string argsString = "")
    {
        await TestableRemindMeCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString
        );
    }

    public async Task TestableRemindMeCommandAsync(
        IIzzyContext context,
        string argsString = "")
    {
        if (argsString == "")
        {
            await context.Channel.SendMessageAsync(
                $"Hey uhh... I think you forgot something... (Missing `time` and `message` parameters, see `{_config.Prefix}help remindme`)");
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);

        var timeType = TimeHelper.GetTimeType(args.Arguments[0]);

        _logger.Log(timeType, level: LogLevel.Trace);

        if (timeType == "unknown" || timeType == "relative")
        {
            if (args.Arguments.Length < (timeType == "unknown" ? 2 : 3))
            {
                await context.Channel.SendMessageAsync("Please provide a time/date!");
                return;
            }

            if (args.Arguments.Length < (timeType == "unknown" ? 3 : 4))
            {
                await context.Channel.SendMessageAsync("You have to tell me what to remind you!");
                return;
            }

            var relativeTimeUnits = new[]
            {
                "years", "year", "months", "month", "days", "day", "weeks", "week", "days", "day", "hours", "hour",
                "minutes", "minute", "seconds", "second"
            };

            var timeNumber = args.Arguments[(timeType == "unknown" ? 0 : 1)];
            var timeUnit = args.Arguments[(timeType == "unknown" ? 1 : 2)];

            if (!int.TryParse(timeNumber, out var time))
            {
                await context.Channel.SendMessageAsync($"I couldn't convert `{timeNumber}` to a number, please try again.");
                return;
            }

            if (!relativeTimeUnits.Contains(timeUnit))
            {
                await context.Channel.SendMessageAsync($"I couldn't convert `{timeUnit}` to a duration type, please try again.");
                return;
            }

            var timeHelperResponse = TimeHelper.Convert($"in {time} {timeUnit}");

            var content = string.Join("", argsString.Skip(args.Indices[(timeType == "unknown" ? 1 : 2)]));
            content = DiscordHelper.StripQuotes(content);

            if (content == "")
            {
                await context.Channel.SendMessageAsync("You have to tell me what to remind you!");
                return;
            }

            _logger.Log($"Adding scheduled job to remind user to \"{content}\" at {timeHelperResponse.Time:F}",
                context: context, level: LogLevel.Debug);
            var action = new ScheduledEchoJob(context.User, content);
            var task = new ScheduledJob(DateTimeOffset.UtcNow,
                timeHelperResponse.Time, action);
            await _schedule.CreateScheduledJob(task);
            _logger.Log($"Added scheduled job for user", context: context, level: LogLevel.Debug);

            await context.Channel.SendMessageAsync($"Okay! I'll DM you a reminder <t:{timeHelperResponse.Time.ToUnixTimeSeconds()}:R>.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"<@186730180872634368> https://www.youtube.com/watch?v=-5wpm-gesOY{Environment.NewLine}(I don't currently support timezones, which is required for the input you just gave me, so I'm telling my primary dev that she has to make me support them)");
            return;
        }
    }

    [Command("rule")]
    [Summary("Show one of our server rules.")]
    [Remarks("Takes the text from FirstRuleMessageId or one of the messages after it, depending on the number given. If the number is a key in HiddenRules, the corresponding value is displayed instead.")]
    [Alias("rules")]
    [Parameter("number", ParameterType.Integer, "The rule number to get.")]
    [ExternalUsageAllowed]
    public async Task RuleCommandAsync([Remainder] string argString = "")
    {
        await TestableRuleCommandAsync(
            new SocketCommandContextAdapter(Context),
            argString
        );
    }

    public async Task TestableRuleCommandAsync(
        IIzzyContext context,
        string argString = "")
    {
        argString = argString.Trim();
        if (argString == "")
        {
            await context.Channel.SendMessageAsync("You need to give me a rule number to look up!");
            return;
        }

        if (_config.HiddenRules.ContainsKey(argString))
        {
            await context.Channel.SendMessageAsync(_config.HiddenRules[argString]);
            return;
        }

        var firstMessageId = _config.FirstRuleMessageId;
        if (firstMessageId == 0)
        {
            await context.Channel.SendMessageAsync("I can't look up rules without knowing where the first one is. Please ask a mod to use `.config FirstRuleMessageId`.");
            return;
        }

        if (int.TryParse(argString, out var ruleNumber))
        {
            var rulesChannel = context.Guild.RulesChannel;

            string ruleMessage;
            if (ruleNumber == 1)
            {
                ruleMessage = (await rulesChannel.GetMessageAsync(firstMessageId)).Content;
            }
            else
            {
                // There might be too few messages in the rules channel, or GetMessagesAsync() might return the messages
                // in a strange order, so we have to gather all messages from rule 2 to rule N and then sort them.
                var rulesAfterFirst = new List<(ulong, string)>();
                await foreach (var messageBatch in rulesChannel.GetMessagesAsync(firstMessageId, Direction.After, ruleNumber - 1))
                {
                    foreach (var message in messageBatch)
                        rulesAfterFirst.Add((message.Id, message.Content));
                }

                if (rulesAfterFirst.Count < (ruleNumber - 1))
                {
                    await context.Channel.SendMessageAsync($"Sorry, there doesn't seem to be a rule {ruleNumber}");
                    return;
                }

                // But we can assume all snowflake ids in Discord are monotonic, i.e. later rules will have higher ids
                // -2 because of 0-indexing plus the fact that these are messages *after* rule 1
                ruleMessage = rulesAfterFirst.OrderBy(t => t.Item1).ElementAt(ruleNumber - 2).Item2;
            }

            await context.Channel.SendMessageAsync(DiscordHelper.TrimDiscordWhitespace(ruleMessage), allowedMentions: AllowedMentions.None);
        }
        else
        {
            await context.Channel.SendMessageAsync($"Sorry, I couldn't convert {argString} to a number.");
        }
    }
}
