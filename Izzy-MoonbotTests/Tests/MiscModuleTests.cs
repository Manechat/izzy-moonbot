using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot;
using Izzy_Moonbot_Tests.Services;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.EventListeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class MiscModuleTests
{
    public static async Task<CommandService> SetupCommandService()
    {
        // Hack to avoid trying to load appsettings in tests
        DevCommandAttribute.TestMode = true;

        var services = new ServiceCollection();

        // Since we're using CommandService purely for its metadata, not the concrete service instances it
        // ends up building, it doesn't matter what values we pass for the state objects or the logger.
        services.AddTransient<ILogger<Worker>, TestLogger<Worker>>();
        services.AddSingleton(new Config());
        services.AddSingleton(new Dictionary<ulong, User>());
        services.AddSingleton(new List<ScheduledJob>());
        services.AddSingleton(new GeneralStorage());
        services.AddSingleton(new State());
        services.AddSingleton(new QuoteStorage());

        services.AddSingleton<ConfigDescriber>();
        services.AddSingleton<LoggingService>();
        services.AddSingleton<ModLoggingService>();
        services.AddSingleton<SpamService>();
        services.AddSingleton<ModService>();
        services.AddSingleton<RaidService>();
        services.AddSingleton<FilterService>();
        services.AddSingleton<ScheduleService>();
        services.AddSingleton<QuoteService>();
        services.AddSingleton(services);
        services.AddSingleton<ConfigListener>();
        services.AddSingleton<UserListener>();
        services.AddSingleton<MessageListener>();
        services.AddHostedService<Worker>();

        var commands = new CommandService();

        // The prod code uses GetEntryAssembly() to get Izzy-Moonbot, but since Izzy-Moonbot-Tests is a different assembly,
        // we have to pick a random type from Izzy-Moonbot to get this to look over there for modules.
        await commands.AddModulesAsync(Assembly.GetAssembly(typeof(MiscModule)), services.BuildServiceProvider());

        return commands;
    }

    public async Task<(ScheduleService, MiscModule)> SetupMiscModule(Config cfg)
    {
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, scheduledJobs);
        var gs = new GeneralStorage();

        var cfgDescriber = new ConfigDescriber();
        return (ss, new MiscModule(cfg, cfgDescriber, ss, logger, modLog, await SetupCommandService(), gs));
    }

    [TestMethod()]
    public async Task RemindMe_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mm) = await SetupMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        await ss.Unicycle(client);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme");
        await mm.TestableRemindMeCommandAsync(context, "");
        Assert.AreEqual("Hey uhh... I think you forgot something... (Missing `time` and `message` parameters, see `.help remindme`)", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme 1 minute test");
        await mm.TestableRemindMeCommandAsync(context, "1 minute test");
        Assert.AreEqual("Okay! I'll DM you a reminder <t:1286668860:R>.", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);

        await ss.Unicycle(client);

        Assert.AreEqual(1, client.DirectMessages[sunny.Id].Count);
        Assert.AreEqual("test", client.DirectMessages[sunny.Id].Last().Content);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }

    [TestMethod()]
    public async Task RemindMe_ExtraSpaces_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mm) = await SetupMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        await ss.Unicycle(client);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme 1     minute test");
        await mm.TestableRemindMeCommandAsync(context, "1     minute test");
        Assert.AreEqual("Okay! I'll DM you a reminder <t:1286668860:R>.", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await ss.Unicycle(client);

        // regression test: the DM would end up saying "ute test" because the extra spaces confused argument parsing
        Assert.AreEqual(1, client.DirectMessages[sunny.Id].Count);
        Assert.AreEqual("test", client.DirectMessages[sunny.Id].Last().Content);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }

    [TestMethod()]
    public async Task RemindMe_DiscordTimestamp_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mm) = await SetupMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme <t:1286668860:R> test");
        await mm.TestableRemindMeCommandAsync(context, "<t:1286668860:R> test");
        Assert.AreEqual("Okay! I'll DM you a reminder <t:1286668860:R>.", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await ss.Unicycle(client);
        Assert.AreEqual("test", client.DirectMessages[sunny.Id].Last().Content);
    }

    [TestMethod()]
    public async Task Rule_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mm) = await SetupMiscModule(cfg);

        guild.RulesChannel = new StubChannel(9999, "rules", new List<StubMessage>
        {
            new StubMessage(0, "Welcome to Maretime Bay!", sunny.Id), // the non-rule message in #rules, so FirstRuleMessageId serves a purpose
            new StubMessage(1, "something about harmony", sunny.Id),
            new StubMessage(2, "\n\nfree smoothies for everypony", sunny.Id),
            new StubMessage(3, ":blank:\n:blank:\nobey the alicorns", sunny.Id),
        });
        cfg.FirstRuleMessageId = 1;

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule");
        await mm.TestableRuleCommandAsync(context, "");
        Assert.AreEqual("You need to give me a rule number to look up!", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule 1");
        await mm.TestableRuleCommandAsync(context, "1");
        Assert.AreEqual("something about harmony", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule 2");
        await mm.TestableRuleCommandAsync(context, "2");
        Assert.AreEqual("free smoothies for everypony", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule 3");
        await mm.TestableRuleCommandAsync(context, "3");
        Assert.AreEqual("obey the alicorns", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule 4");
        await mm.TestableRuleCommandAsync(context, "4");
        Assert.AreEqual("Sorry, there doesn't seem to be a rule 4", generalChannel.Messages.Last().Content);

        cfg.HiddenRules.Add("4", "blame Sprout");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".rule 4");
        await mm.TestableRuleCommandAsync(context, "4");
        Assert.AreEqual("blame Sprout", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task HelpCommand_BreathingTestsAsync()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);
        cfg.ModRole = roles[0].Id;

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help");
        await mm.TestableHelpCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Run `.help <category>` to");
        StringAssert.Contains(description, "Run `.help <command>` to");
        StringAssert.Contains(description, "list of all the categories");
        StringAssert.Contains(description, "raid - ");
        StringAssert.Contains(description, "spam - ");
        StringAssert.Contains(description, "ℹ  **See also: `.config`");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help modcore");
        await mm.TestableHelpCommandAsync(context, "modcore");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "list of all the commands");
        StringAssert.Contains(description, "assignrole - ");
        StringAssert.Contains(description, "ban - ");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help ban");
        await mm.TestableHelpCommandAsync(context, "ban");

        description = generalChannel.Messages.Last().Content;
        // StringAssert.Contains is broken for strings with {}s, but explicitly passing some nulls works around that
        StringAssert.Contains(description, "**.ban** - ModCore category", null, null);
        StringAssert.Contains(description, "ℹ  *This is a moderator", null, null);
        StringAssert.Contains(description, "*Bans a user", null, null);
        StringAssert.Contains(description, "Syntax: `.ban user [duration]`", null, null);
        StringAssert.Contains(description, "user [User ID", null, null);
        StringAssert.Contains(description, "duration [Date/Time] {OPTIONAL}", null, null);
        StringAssert.Contains(description, "Example: ", null, null);
    }

    [TestMethod()]
    public async Task HelpCommand_Aliases_TestsAsync()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);
        cfg.ModRole = roles[0].Id;

        // Check .help's regular behavior before adding aliases
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help addquote");
        await mm.TestableHelpCommandAsync(context, "addquote");

        var baseAddQuoteDescription = generalChannel.Messages.Last().Content;
        Assert.IsFalse(baseAddQuoteDescription.Contains("Relevant aliases:"));

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help moonlaser");
        await mm.TestableHelpCommandAsync(context, "moonlaser");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Sorry, I was unable to", null, null);
        StringAssert.Contains(description, "\"moonlaser\"", null, null);

        cfg.Aliases.Add("moonlaser", "addquote moon");
        cfg.Aliases.Add("sayhi", "echo <#1> hi");
        cfg.Aliases.Add("crown", "assignrole <@1>");

        // .help should now append a Relevant Aliases line for commands with an alias
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help addquote");
        await mm.TestableHelpCommandAsync(context, "addquote");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, baseAddQuoteDescription, null, null);
        StringAssert.EndsWith(description, "Relevant aliases: .moonlaser", null, null);

        // .help <alias> should now prepend the alias definition to the help for the underlying command
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help moonlaser");
        await mm.TestableHelpCommandAsync(context, "moonlaser");

        description = generalChannel.Messages.Last().Content;
        StringAssert.StartsWith(description, "**.moonlaser** is an alias for **.addquote moon** (see .config Aliases)", null, null);
        StringAssert.Contains(description, baseAddQuoteDescription, null, null);

        // regression test: .help ass was mistakenly printing .assignrole's aliases because ass is a prefix of assignrole
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help ass");
        await mm.TestableHelpCommandAsync(context, "ass");

        Assert.IsFalse(generalChannel.Messages.Last().Content.Contains("Relevant aliases:"));
    }

    [TestMethod()]
    public async Task HelpCommand_AliasesAreLowPriority_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id;
        // Adding a ".ban" alias has no effect on the output of ".help ban", because ".ban" is already a command
        cfg.Aliases.Add("ban", "echo bye-bye");

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help ban");
        await mm.TestableHelpCommandAsync(context, "ban");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**.ban** - ModCore category", null, null);
        StringAssert.Contains(description, "ℹ  *This is a moderator", null, null);
        StringAssert.Contains(description, "*Bans a user", null, null);
        StringAssert.Contains(description, "Syntax: `.ban user [duration]`", null, null);
        StringAssert.Contains(description, "user [User ID", null, null);
        StringAssert.Contains(description, "duration [Date/Time] {OPTIONAL}", null, null);
        StringAssert.Contains(description, "Example: ", null, null);
    }

    [TestMethod()]
    public async Task HelpCommand_RegularUsers_ModOnlyCommands_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        // Mod-only command

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help ban");
        await mm.TestableHelpCommandAsync(context, "ban");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**.ban** - ModCore category", null, null);
        StringAssert.Contains(description, "ℹ  *This is a moderator", null, null);
        StringAssert.Contains(description, "*Bans a user", null, null);
        StringAssert.Contains(description, "Syntax: `.ban user [duration]`", null, null);
        StringAssert.Contains(description, "user [User ID", null, null);
        StringAssert.Contains(description, "duration [Date/Time] {OPTIONAL}", null, null);
        StringAssert.Contains(description, "Example: ", null, null);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help ban");
        await mm.TestableHelpCommandAsync(context, "ban");

        Assert.AreEqual("Sorry, you don't have permission to use the .ban command.", generalChannel.Messages.Last().Content);

        // Alternate name for a mod-only command

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help rmquote");
        await mm.TestableHelpCommandAsync(context, "rmquote");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**.rmquote** (alternate name of **.removequote**) - Quotes category", null, null);
        StringAssert.Contains(description, "ℹ  *This is a moderator", null, null);
        StringAssert.Contains(description, "*Removes a quote from a user or category", null, null);
        StringAssert.Contains(description, "Syntax: `.removequote user id`", null, null);
        StringAssert.Contains(description, "user [User ID", null, null);
        StringAssert.Contains(description, "id [Integer]", null, null);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help rmquote");
        await mm.TestableHelpCommandAsync(context, "rmquote");

        Assert.AreEqual("Sorry, you don't have permission to use the .rmquote command.", generalChannel.Messages.Last().Content);

        // Alias for a mod-only command

        cfg.Aliases.Add("b", "ban");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help b");
        await mm.TestableHelpCommandAsync(context, "b");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**.b** is an alias for **.ban** (see .config Aliases)", null, null);
        StringAssert.Contains(description, "**.ban** - ModCore category", null, null);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help b");
        await mm.TestableHelpCommandAsync(context, "b");

        Assert.AreEqual("Sorry, you don't have permission to use the .b command.", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task HelpCommand_RegularUsers_ListingCommands_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        // For regular users, command categories don't exist, because they have so few commands it's not worth it

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help modcore");
        await mm.TestableHelpCommandAsync(context, "modcore");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"modcore\" that you have access to.\n" +
                        "\n" +
                        // .userinfo is the only public command in the 'modcore' category
                        "I also see \"modcore\" in the output of: `.help userinfo`", generalChannel.Messages.Last().Content);

        // So just `.help` with no args lists not categories, but the commands regular users can run

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help");
        await mm.TestableHelpCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.StartsWith(description, "Hii! Here's a list of all the commands you can run!");
        StringAssert.Contains(description, "help -");
        StringAssert.Contains(description, "about -");
        StringAssert.Contains(description, "banner -");
        StringAssert.Contains(description, "remindme -");
        StringAssert.Contains(description, "quote -");
        StringAssert.Contains(description, "Run `.help <command>` for help regarding a specific command!");
    }

    [TestMethod()]
    public async Task HelpCommand_CommandSuggestions_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        // moderator gets suggestions for mod-only commands

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help assign");
        await mm.TestableHelpCommandAsync(context, "assign");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"assign\" that you have access to." +
            "\nDid you mean `.assignrole` or `.assoff`?", generalChannel.Messages.Last().Content);

        // regular user does not get suggested mod-only commands

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help assign");
        await mm.TestableHelpCommandAsync(context, "assign");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"assign\" that you have access to.", generalChannel.Messages.Last().Content);

        // everyone gets suggested public commands

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help reminder");
        await mm.TestableHelpCommandAsync(context, "reminder");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"reminder\" that you have access to." +
            "\nDid you mean `.remindme` or `.remind`?", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help reminder");
        await mm.TestableHelpCommandAsync(context, "reminder");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"reminder\" that you have access to." +
            "\nDid you mean `.remindme`?", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task HelpCommand_AliasSuggestions_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        cfg.Aliases.Add("moonlaser", "addquote moon");
        cfg.Aliases.Add("telescope", "quote moon");

        // moderator gets suggestions for mod-only aliases

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help laser");
        await mm.TestableHelpCommandAsync(context, "laser");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"laser\" that you have access to." +
            "\nDid you mean `.banner` or `.moonlaser`?", generalChannel.Messages.Last().Content);

        // regular user does not get suggested mod-only aliases

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help laser");
        await mm.TestableHelpCommandAsync(context, "laser");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"laser\" that you have access to." +
            "\nDid you mean `.banner`?", generalChannel.Messages.Last().Content);

        // everyone gets suggested public aliases

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help telescop");
        await mm.TestableHelpCommandAsync(context, "telescop");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"telescop\" that you have access to." +
            "\nDid you mean `.telescope`?", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help telescop");
        await mm.TestableHelpCommandAsync(context, "telescop");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"telescop\" that you have access to." +
            "\nDid you mean `.telescope`?", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task HelpCommand_CategorySuggestions_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        // moderator gets category suggestions

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help core");
        await mm.TestableHelpCommandAsync(context, "core");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"core\" that you have access to." +
            "\nDid you mean `.modcore`?" +
            "\n" +
            "\nI also see \"core\" in the output of: `.help config` and `.help userinfo` and `.help ban` and `.help banall` and `.help assignrole` and `.help wipe`", generalChannel.Messages.Last().Content);

        // regular user does not

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help core");
        await mm.TestableHelpCommandAsync(context, "core");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"core\" that you have access to." +
            "\n" +
            "\nI also see \"core\" in the output of: `.help userinfo`", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task HelpCommand_SearchDocsAndConfig_Async()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var (_, mm) = await SetupMiscModule(cfg);

        cfg.ModRole = roles[0].Id; // Sunny is a moderator
        var pippId = guild.Users[3].Id; // Pipp is NOT a moderator

        // regular users don't get this extra searching

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, ".help new pony");
        await mm.TestableHelpCommandAsync(context, "new pony");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"new pony\" that you have access to.", generalChannel.Messages.Last().Content);

        // command documentation is searched

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help new pony");
        await mm.TestableHelpCommandAsync(context, "new pony");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"new pony\" that you have access to." +
            "\n" +
            "\nI also see \"new pony\" in the output of: `.help permanp`", generalChannel.Messages.Last().Content);

        // config documentation is searched

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help rotate");
        await mm.TestableHelpCommandAsync(context, "rotate");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"rotate\" that you have access to." +
            "\n" +
            "\nI also see \"rotate\" in the output of: `.config BannerMode` and `.config BannerInterval` and `.config BannerImages`", generalChannel.Messages.Last().Content);

        // for simple values, the searched config documentation includes the current value

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".help 100");
        await mm.TestableHelpCommandAsync(context, "100");

        Assert.AreEqual("Sorry, I was unable to find any command, category, or alias named \"100\" that you have access to." +
            "\n" +
            "\nI also see \"100\" in the output of: `.help rollforbestpony` and `.config UnicycleInterval`", generalChannel.Messages.Last().Content);
    }
}