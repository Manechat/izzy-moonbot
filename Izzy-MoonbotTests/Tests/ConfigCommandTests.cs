﻿using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.EventListeners.ConfigListener;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class ConfigCommandTests
{
    [TestMethod()]
    public async Task ConfigCommand_BreathingTestsAsync()
    {
        var (cfg, cd, (izzyHerself, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        // post ".config asdf *"

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config asdf *");

        Assert.AreEqual(cfg.Prefix, '.');
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "asdf", "*");
        Assert.AreEqual(cfg.Prefix, '.');

        Assert.AreEqual(2, generalChannel.Messages.Count);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[0].AuthorId);
        Assert.AreEqual(".config asdf *", generalChannel.Messages[0].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[1].AuthorId);
        Assert.AreEqual("Sorry, I couldn't find a config value or category called `asdf`!", generalChannel.Messages[1].Content);

        // post ".config Prefix *"

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Prefix *");

        Assert.AreEqual(cfg.Prefix, '.');
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Prefix", "*");
        Assert.AreEqual(cfg.Prefix, '*');

        Assert.AreEqual(4, generalChannel.Messages.Count);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[0].AuthorId);
        Assert.AreEqual(".config asdf *", generalChannel.Messages[0].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[1].AuthorId);
        Assert.AreEqual("Sorry, I couldn't find a config value or category called `asdf`!", generalChannel.Messages[1].Content);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[2].AuthorId);
        Assert.AreEqual(".config Prefix *", generalChannel.Messages[2].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[3].AuthorId);
        Assert.AreEqual("I've set `Prefix` to the following content: *", generalChannel.Messages[3].Content);
    }

    [TestMethod()]
    public async Task ConfigCommand_CategoryTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config misc");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "misc", "");

        Assert.IsTrue(generalChannel.Messages.Last().Content.Contains("Here's a list of all the config items I could find in the Misc category!"));

        // TODO: pagination testing
    }

    [TestMethod()]
    public async Task ConfigCommand_ItemDescriptionsAndGetters_ScalarsTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        // String/Char/Boolean/Integer/Double share the same logic

        Assert.AreEqual(cfg.UnicycleInterval, 100);
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config UnicycleInterval");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "UnicycleInterval", "");
        Assert.AreEqual(cfg.UnicycleInterval, 100);

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "UnicycleInterval");
        StringAssert.Contains(description, "Integer");
        StringAssert.Contains(description, "Misc category");
        StringAssert.Contains(description, "How often, in milliseconds");
        StringAssert.Contains(description, "Current value: `100`");
        StringAssert.Contains(description, "Run `.config UnicycleInterval <value>`");

        // Enum

        Assert.AreEqual(cfg.BannerMode, BannerMode.None);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config BannerMode");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "BannerMode", "");
        Assert.AreEqual(cfg.BannerMode, BannerMode.None);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "BannerMode");
        StringAssert.Contains(description, "Enum");
        StringAssert.Contains(description, "Banner category");
        StringAssert.Contains(description, "If and how I will manage the server banner");
        StringAssert.Contains(description, "`None`, `Rotate`, `Shuffle`, `ManebooruFeatured`");
        StringAssert.Contains(description, "Current value: `None`");
        StringAssert.Contains(description, "Run `.config BannerMode <value>`");

        // Role/Channel are mostly the same

        cfg.ModRole = 1234ul;
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config ModRole");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "ModRole", "");
        Assert.AreEqual(cfg.ModRole, 1234ul);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "ModRole");
        StringAssert.Contains(description, "Role");
        StringAssert.Contains(description, "Setup category");
        StringAssert.Contains(description, "The role that I allow");
        StringAssert.Contains(description, "Current value: <@&1234>"); // this is the difference
        StringAssert.Contains(description, "Run `.config ModRole <value>`");

        cfg.ModChannel = 42ul;
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config ModChannel");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "ModChannel", "");
        Assert.AreEqual(cfg.ModChannel, 42ul);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "ModChannel");
        StringAssert.Contains(description, "Channel");
        StringAssert.Contains(description, "Setup category");
        StringAssert.Contains(description, "The channel where I'll post");
        StringAssert.Contains(description, "Current value: <#42>"); // this is the difference
        StringAssert.Contains(description, "Run `.config ModChannel <value>`");
    }

    [TestMethod()]
    public async Task ConfigCommand_ItemDescriptionsAndGetters_CollectionsTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        // StringSet

        cfg.MentionResponses.Add("hello new friend!");
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "");
        TestUtils.AssertSetsAreEqual(cfg.MentionResponses, new HashSet<string> { "hello new friend!" });

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "MentionResponses");
        StringAssert.Contains(description, "List of Strings");
        StringAssert.Contains(description, "Misc category");
        StringAssert.Contains(description, "A list of responses I will");
        StringAssert.Contains(description, "Run `.config MentionResponses list` to");
        StringAssert.Contains(description, "Run `.config MentionResponses add <value>` to");
        StringAssert.Contains(description, "Run `.config MentionResponses remove <value>` to");
        StringAssert.Contains(description, "Run `.config MentionResponses clear` to");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses list");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "list");
        TestUtils.AssertSetsAreEqual(cfg.MentionResponses, new HashSet<string> { "hello new friend!" });

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "MentionResponses");
        StringAssert.Contains(description, "the following values:");
        StringAssert.Contains(description, $"```\nhello new friend!\n```");

        // StringDictionary

        cfg.Aliases.Add("moonlaser", "addquote moon");
        cfg.Aliases.Add("echogeneral", "echo <#1>");
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" }, { "echogeneral", "echo <#1>" } }, cfg.Aliases);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Aliases");
        StringAssert.Contains(description, "Map of String");
        StringAssert.Contains(description, "Misc category");
        StringAssert.Contains(description, "Shorthand commands which");
        StringAssert.Contains(description, "Run `.config Aliases list` to");
        StringAssert.Contains(description, "Run `.config Aliases get <key>` to");
        StringAssert.Contains(description, "Run `.config Aliases set <key> <value>` to");
        StringAssert.Contains(description, "Run `.config Aliases delete <key>` to");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases list");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "list");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" }, { "echogeneral", "echo <#1>" } }, cfg.Aliases);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Aliases");
        StringAssert.Contains(description, "the following keys:");
        StringAssert.Contains(description, $"```\n" +
            $"moonlaser = addquote moon\n" +
            $"echogeneral = echo <#1>\n" +
            $"```");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases get moonlaser");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "get moonlaser");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" }, { "echogeneral", "echo <#1>" } }, cfg.Aliases);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**moonlaser** contains");
        StringAssert.Contains(description, $": `addquote moon`");

        // StringSetDictionary

        cfg.FilteredWords.Add("slurs", new HashSet<string> { "mudpony", "screwhead" });
        cfg.FilteredWords.Add("links", new HashSet<string> { "cliptrot.pony/" });
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "mudpony", "screwhead" } },
            { "links", new HashSet<string> { "cliptrot.pony/" } }
        }, cfg.FilteredWords);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "FilteredWords");
        StringAssert.Contains(description, "Map of Lists of Strings");
        StringAssert.Contains(description, "Filter category");
        StringAssert.Contains(description, "I'll delete any message containing one of these words");
        StringAssert.Contains(description, "Run `.config FilteredWords list` to");
        StringAssert.Contains(description, "Run `.config FilteredWords get <key>` to");
        StringAssert.Contains(description, "Run `.config FilteredWords add <key> <value>` to");
        StringAssert.Contains(description, "Run `.config FilteredWords deleteitem <key> <value>` to");
        StringAssert.Contains(description, "Run `.config FilteredWords deletelist <key>` to");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords list");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "list");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "mudpony", "screwhead" } },
            { "links", new HashSet<string> { "cliptrot.pony/" } }
        }, cfg.FilteredWords);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "FilteredWords");
        StringAssert.Contains(description, "the following keys:");
        StringAssert.Contains(description, $"```\n" +
            $"slurs (2 entries)\n" +
            $"\n" +
            $"links (1 entries)\n" +
            $"\n" +
            $"```");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords get slurs");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "get slurs");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "mudpony", "screwhead" } },
            { "links", new HashSet<string> { "cliptrot.pony/" } }
        }, cfg.FilteredWords);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "**slurs** contains");
        StringAssert.Contains(description, $"```\n" +
            $"mudpony\n" +
            $"screwhead\n" +
            $"```");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditStringSetTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        cfg.MentionResponses.Add("hello new friend!");
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses add got something I can unicycle?");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "add got something I can unicycle?");
        TestUtils.AssertSetsAreEqual(cfg.MentionResponses, new HashSet<string> { "hello new friend!", "got something I can unicycle?" });

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "MentionResponses");
        StringAssert.Contains(description, "I added the following");
        StringAssert.Contains(description, $"```\ngot something I can unicycle?\n```");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses remove hello new friend!");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "remove hello new friend!");
        TestUtils.AssertSetsAreEqual(cfg.MentionResponses, new HashSet<string> { "got something I can unicycle?" });

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "MentionResponses");
        StringAssert.Contains(description, "I removed the following");
        StringAssert.Contains(description, $"```\nhello new friend!\n```");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses clear");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "clear");
        TestUtils.AssertSetsAreEqual(cfg.MentionResponses, new HashSet<string>{});

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "MentionResponses");
        StringAssert.Contains(description, "I've cleared");
        StringAssert.Contains(description, $"```\ngot something I can unicycle?\n```");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditRoleSetTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        cfg.SpamBypassRoles.Add(2ul);
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBypassRoles add <@&1>");
    
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamBypassRoles", "add <@&1>");
    
        TestUtils.AssertSetsAreEqual(cfg.SpamBypassRoles, new HashSet<ulong> { 2ul, 1ul });

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "SpamBypassRoles");
        StringAssert.Contains(description, "I added the following");
        StringAssert.Contains(description, $"\nAlicorn");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBypassRoles remove <@&2>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamBypassRoles", "remove <@&2>");
        TestUtils.AssertSetsAreEqual(cfg.SpamBypassRoles, new HashSet<ulong> { 1ul });

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "SpamBypassRoles");
        StringAssert.Contains(description, "I removed the following");
        StringAssert.Contains(description, $"\nPegasus");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditChannelSetTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();

        cfg.SpamIgnoredChannels.Add(modChat.Id);
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".config SpamIgnoredChannels add <#{generalChannel.Id}>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamIgnoredChannels", $"add <#{generalChannel.Id}>");
        TestUtils.AssertSetsAreEqual(cfg.SpamIgnoredChannels, new HashSet<ulong> { modChat.Id, generalChannel.Id });

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "SpamIgnoredChannels");
        StringAssert.Contains(description, "I added the following");
        StringAssert.Contains(description, $"\ngeneral");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".config SpamIgnoredChannels remove <#{modChat.Id}>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamIgnoredChannels", $"remove <#{modChat.Id}>");
        TestUtils.AssertSetsAreEqual(cfg.SpamIgnoredChannels, new HashSet<ulong> { generalChannel.Id });

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "SpamIgnoredChannels");
        StringAssert.Contains(description, "I removed the following");
        StringAssert.Contains(description, $"\nmodchat");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditStringDictionaryTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        cfg.Aliases.Add("moonlaser", "addquote moon");
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases set echogeneral echo <#1>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "set echogeneral echo <#1>");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" }, { "echogeneral", "echo <#1>" } }, cfg.Aliases);

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Aliases");
        StringAssert.Contains(description, "I added the following");
        StringAssert.Contains(description, "the `echogeneral` map key");
        StringAssert.Contains(description, ": `echo <#1>`");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases set moonlaser addquote theothermoon");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "set moonlaser addquote theothermoon");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote theothermoon" }, { "echogeneral", "echo <#1>" } }, cfg.Aliases);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Aliases");
        StringAssert.Contains(description, "I changed the");
        StringAssert.Contains(description, "the `moonlaser` map key");
        StringAssert.Contains(description, "from `addquote moon` to `addquote theothermoon`");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases delete echogeneral");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "delete echogeneral");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote theothermoon" } }, cfg.Aliases);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Aliases");
        StringAssert.Contains(description, "I removed the");
        StringAssert.Contains(description, ": `echogeneral`");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditStringSetDictionaryTestsAsync()
    {
        var (cfg, cd, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        cfg.FilteredWords.Add("slurs", new HashSet<string> { "mudpony" });
        cfg.FilteredWords.Add("links", new HashSet<string> { "cliptrot.pony/" });
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords add slurs screwhead");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "add slurs screwhead");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "mudpony", "screwhead" } },
            { "links", new HashSet<string> { "cliptrot.pony/" } }
        }, cfg.FilteredWords);

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "I added the following");
        StringAssert.Contains(description, "the `slurs` string list");
        StringAssert.Contains(description, ": `screwhead`");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords deleteitem slurs mudpony");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "deleteitem slurs mudpony");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "screwhead" } },
            { "links", new HashSet<string> { "cliptrot.pony/" } }
        }, cfg.FilteredWords);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "I removed the");
        StringAssert.Contains(description, "the `slurs` string list");
        StringAssert.Contains(description, ": `mudpony`");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords deletelist links");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "deletelist links");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> {
            { "slurs", new HashSet<string> { "screwhead" } }
        }, cfg.FilteredWords);

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "I deleted the string list");
        StringAssert.Contains(description, ": links");
    }

    [TestMethod()]
    public async Task ConfigCommand_EditEveryItemAsync()
    {
        var izzyHerself = new TestUser("Izzy Moonbot", 1);
        var sunny = new TestUser("Sunny", 2);
        var users = new List<TestUser> { izzyHerself, sunny };

        var alicorn = new TestRole("Alicorn", 1);
        var pony = new TestRole("Pony", 2);
        var newPony = new TestRole("New Pony", 3);
        var roles = new List<TestRole> { alicorn, pony, newPony };

        var generalChannel = new StubChannel(1, "general");
        var modChannel = new StubChannel(2, "modchat");
        var logChannel = new StubChannel(3, "bot-logs");
        var channels = new List<StubChannel> { generalChannel, modChannel, logChannel };

        var guild = new StubGuild(1, "Maretime Bay", roles, users, channels);
        var client = new StubClient(izzyHerself, new List<StubGuild> { guild });

        var cfg = new Config();
        var cd = new ConfigDescriber();


        // post ".config Prefix *"
        Assert.AreEqual(cfg.Prefix, '.');
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config prefix *");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Prefix", "*");
        Assert.AreEqual(cfg.Prefix, '*');
        Assert.AreEqual("I've set `Prefix` to the following content: *", generalChannel.Messages.Last().Content);

        // post ".config UnicycleInterval 42"
        Assert.AreEqual(cfg.UnicycleInterval, 100);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config UnicycleInterval 42");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "UnicycleInterval", "42");
        Assert.AreEqual(cfg.UnicycleInterval, 42);
        Assert.AreEqual("I've set `UnicycleInterval` to the following content: 42", generalChannel.Messages.Last().Content);

        // post ".config MentionResponseEnabled true"
        Assert.AreEqual(cfg.MentionResponseEnabled, false);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponseEnabled true");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponseEnabled", "true");
        Assert.AreEqual(cfg.MentionResponseEnabled, true);
        Assert.AreEqual("I've set `MentionResponseEnabled` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config MentionResponses add yes i am bot"
        TestUtils.AssertSetsAreEqual(new HashSet<string>(), cfg.MentionResponses);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses add yes i am bot");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "add yes i am bot");
        TestUtils.AssertSetsAreEqual(new HashSet<string> { "yes i am bot" }, cfg.MentionResponses);
        Assert.AreEqual($"I added the following content to the `MentionResponses` string list:\n" +
            $"```\n" +
            $"yes i am bot\n" +
            $"```",
            generalChannel.Messages.Last().Content);

        // post ".config MentionResponseCooldown 50"
        Assert.AreEqual(cfg.MentionResponseCooldown, 600);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponseCooldown 50");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MentionResponseCooldown", "50");
        Assert.AreEqual(cfg.MentionResponseCooldown, 50);
        Assert.AreEqual("I've set `MentionResponseCooldown` to the following content: 50", generalChannel.Messages.Last().Content);

        // post ".config DiscordActivityName buckball"
        Assert.AreEqual(cfg.DiscordActivityName, "you all soon");
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config DiscordActivityName buckball");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "DiscordActivityName", "buckball");
        Assert.AreEqual(cfg.DiscordActivityName, "buckball");
        Assert.AreEqual("I've set `DiscordActivityName` to the following content: buckball", generalChannel.Messages.Last().Content);

        // post ".config DiscordActivityWatching false"
        Assert.AreEqual(cfg.DiscordActivityWatching, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config DiscordActivityWatching false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "DiscordActivityWatching", "false");
        Assert.AreEqual(cfg.DiscordActivityWatching, false);
        Assert.AreEqual("I've set `DiscordActivityWatching` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config Aliases set moonlaser addquote moon"
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases set moonlaser addquote moon");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "set moonlaser addquote moon");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" } }, cfg.Aliases);
        Assert.AreEqual("I added the following string to the `moonlaser` map key in the `Aliases` map: `addquote moon`", generalChannel.Messages.Last().Content);

        // post ".config FirstRuleMessageId 1234"
        Assert.AreEqual(cfg.FirstRuleMessageId, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FirstRuleMessageId 1234");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FirstRuleMessageId", "1234");
        Assert.AreEqual(cfg.FirstRuleMessageId, 1234ul);
        Assert.AreEqual("I've set `FirstRuleMessageId` to the following content: 1234", generalChannel.Messages.Last().Content);

        // post ".config HiddenRules set -1 :twiactually:"
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.HiddenRules);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config HiddenRules set -1 :twiactually:");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "HiddenRules", "set -1 :twiactually:");
        TestUtils.AssertDictionariesAreEqual(new Dictionary<string, string> { { "-1", ":twiactually:" } }, cfg.HiddenRules);
        Assert.AreEqual("I added the following string to the `-1` map key in the `HiddenRules` map: `:twiactually:`", generalChannel.Messages.Last().Content);

        // post ".config BannerMode ManebooruFeatured"
        Assert.AreEqual(cfg.BannerMode, BannerMode.None);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config BannerMode ManebooruFeatured");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "BannerMode", "ManebooruFeatured");
        Assert.AreEqual(cfg.BannerMode, BannerMode.ManebooruFeatured);
        Assert.AreEqual("I've set `BannerMode` to the following content: ManebooruFeatured", generalChannel.Messages.Last().Content);

        // post ".config BannerInterval 1"
        Assert.AreEqual(cfg.BannerInterval, 60);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config BannerInterval 1");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "BannerInterval", "1");
        Assert.AreEqual(cfg.BannerInterval, 1);
        Assert.AreEqual("I've set `BannerInterval` to the following content: 1", generalChannel.Messages.Last().Content);

        // post ".config BannerImages add https://static.manebooru.art/img/2022/11/23/4025857/large.png"
        TestUtils.AssertSetsAreEqual(new HashSet<string>(), cfg.BannerImages);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config BannerImages add https://static.manebooru.art/img/2022/11/23/4025857/large.png");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "BannerImages", "add https://static.manebooru.art/img/2022/11/23/4025857/large.png");
        TestUtils.AssertSetsAreEqual(new HashSet<string> { "https://static.manebooru.art/img/2022/11/23/4025857/large.png" }, cfg.BannerImages);
        Assert.AreEqual($"I added the following content to the `BannerImages` string list:\n" +
            $"```\n" +
            $"https://static.manebooru.art/img/2022/11/23/4025857/large.png\n" +
            $"```",
            generalChannel.Messages.Last().Content);

        // post ".config ModRole <@&1>"
        Assert.AreEqual(cfg.ModRole, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config ModRole <@&1>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "ModRole", "<@&1>");
        Assert.AreEqual(cfg.ModRole, 1ul);
        Assert.AreEqual("I've set `ModRole` to the following content: <@&1>", generalChannel.Messages.Last().Content);

        // post ".config ModChannel <#2>"
        Assert.AreEqual(cfg.ModChannel, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config ModChannel <#2>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "ModChannel", "<#2>");
        Assert.AreEqual(cfg.ModChannel, 2ul);
        Assert.AreEqual("I've set `ModChannel` to the following content: <#2>", generalChannel.Messages.Last().Content);

        // post ".config LogChannel <#3>"
        Assert.AreEqual(cfg.LogChannel, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config LogChannel <#3>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "LogChannel", "<#3>");
        Assert.AreEqual(cfg.LogChannel, 3ul);
        Assert.AreEqual("I've set `LogChannel` to the following content: <#3>", generalChannel.Messages.Last().Content);

        // post ".config ManageNewUserRoles true"
        Assert.AreEqual(cfg.ManageNewUserRoles, false);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config ManageNewUserRoles true");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "ManageNewUserRoles", "true");
        Assert.AreEqual(cfg.ManageNewUserRoles, true);
        Assert.AreEqual("I've set `ManageNewUserRoles` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config MemberRole <@&2>"
        Assert.AreEqual(cfg.MemberRole, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config MemberRole <@&2>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "MemberRole", "<@&2>");
        Assert.AreEqual(cfg.MemberRole, 2ul);
        Assert.AreEqual("I've set `MemberRole` to the following content: <@&2>", generalChannel.Messages.Last().Content);

        // post ".config NewMemberRole <@&3>"
        Assert.AreEqual(cfg.NewMemberRole, 0ul);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config NewMemberRole <@&3>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "NewMemberRole", "<@&3>");
        Assert.AreEqual(cfg.NewMemberRole, 3ul);
        Assert.AreEqual("I've set `NewMemberRole` to the following content: <@&3>", generalChannel.Messages.Last().Content);

        // post ".config NewMemberRoleDecay 120"
        Assert.AreEqual(cfg.NewMemberRoleDecay, 0);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config NewMemberRoleDecay 120");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "NewMemberRoleDecay", "120");
        Assert.AreEqual(cfg.NewMemberRoleDecay, 120);
        Assert.AreEqual("I've set `NewMemberRoleDecay` to the following content: 120", generalChannel.Messages.Last().Content);

        // post ".config RolesToReapplyOnRejoin add <@&3>"
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), cfg.RolesToReapplyOnRejoin);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config RolesToReapplyOnRejoin add <@&3>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "RolesToReapplyOnRejoin", "add <@&3>");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { 3ul }, cfg.RolesToReapplyOnRejoin);
        Assert.AreEqual($"I added the following content to the `RolesToReapplyOnRejoin` role list:\n" +
            $"New Pony",
            generalChannel.Messages.Last().Content);

        // post ".config FilterEnabled false"
        Assert.AreEqual(cfg.FilterEnabled, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilterEnabled false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilterEnabled", "false");
        Assert.AreEqual(cfg.FilterEnabled, false);
        Assert.AreEqual("I've set `FilterEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config FilterIgnoredChannels add <#2>"
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), cfg.FilterIgnoredChannels);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilterIgnoredChannels add <#2>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilterIgnoredChannels", "add <#2>");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { 2ul }, cfg.FilterIgnoredChannels);
        Assert.AreEqual($"I added the following content to the `FilterIgnoredChannels` channel list:\n" +
            $"modchat",
            generalChannel.Messages.Last().Content);

        // post ".config FilterBypassRoles add <@&1>"
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), cfg.FilterBypassRoles);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilterBypassRoles add <@&1>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilterBypassRoles", "add <@&1>");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { 1ul }, cfg.FilterBypassRoles);
        Assert.AreEqual($"I added the following content to the `FilterBypassRoles` role list:\n" +
            $"Alicorn",
            generalChannel.Messages.Last().Content);

        // post ".config FilterDevBypass false"
        Assert.AreEqual(cfg.FilterDevBypass, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilterDevBypass false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilterDevBypass", "false");
        Assert.AreEqual(cfg.FilterDevBypass, false);
        Assert.AreEqual("I've set `FilterDevBypass` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config FilterWords add mudpony"
        TestUtils.AssertSetsAreEqual(new HashSet<string>(), cfg.FilterWords);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilterWords add mudpony");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilterWords", "add mudpony");
        TestUtils.AssertSetsAreEqual(new HashSet<string> { "mudpony" }, cfg.FilterWords);
        Assert.AreEqual("I added the following content to the `FilterWords` string list:\n```\nmudpony\n```", generalChannel.Messages.Last().Content);

        // post ".config FilteredWords add slurs mudpony"
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>>(), cfg.FilteredWords);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords add slurs mudpony");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "add slurs mudpony");
        TestUtils.AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> { { "slurs", new HashSet<string> { "mudpony" } } }, cfg.FilteredWords);
        Assert.AreEqual("I added the following string to the `slurs` string list in the `FilteredWords` map: `mudpony`", generalChannel.Messages.Last().Content);

        // post ".config SpamEnabled false"
        Assert.AreEqual(cfg.SpamEnabled, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamEnabled false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamEnabled", "false");
        Assert.AreEqual(cfg.SpamEnabled, false);
        Assert.AreEqual("I've set `SpamEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config SpamBypassRoles add <@&1>"
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), cfg.SpamBypassRoles);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBypassRoles add <@&1>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamBypassRoles", "add <@&1>");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { 1ul }, cfg.SpamBypassRoles);
        Assert.AreEqual($"I added the following content to the `SpamBypassRoles` role list:\n" +
            $"Alicorn",
            generalChannel.Messages.Last().Content);

        // post ".config SpamIgnoredChannels add <#2>"
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), cfg.SpamIgnoredChannels);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamIgnoredChannels add <#2>");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamIgnoredChannels", "add <#2>");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { 2ul }, cfg.SpamIgnoredChannels);
        Assert.AreEqual($"I added the following content to the `SpamIgnoredChannels` channel list:\n" +
            $"modchat",
            generalChannel.Messages.Last().Content);

        // post ".config SpamDevBypass false"
        Assert.AreEqual(cfg.SpamDevBypass, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamDevBypass false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamDevBypass", "false");
        Assert.AreEqual(cfg.SpamDevBypass, false);
        Assert.AreEqual("I've set `SpamDevBypass` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config SpamBasePressure 15"
        Assert.AreEqual(cfg.SpamBasePressure, 10.0);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBasePressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamBasePressure", "15");
        Assert.AreEqual(cfg.SpamBasePressure, 15.0);
        Assert.AreEqual("I've set `SpamBasePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamImagePressure 15"
        Assert.AreEqual(cfg.SpamImagePressure, 8.3);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamImagePressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamImagePressure", "15");
        Assert.AreEqual(cfg.SpamImagePressure, 15.0);
        Assert.AreEqual("I've set `SpamImagePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamLengthPressure 15"
        Assert.AreEqual(cfg.SpamLengthPressure, 0.00625);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamLengthPressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamLengthPressure", "15");
        Assert.AreEqual(cfg.SpamLengthPressure, 15.0);
        Assert.AreEqual("I've set `SpamLengthPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamLinePressure 15"
        Assert.AreEqual(cfg.SpamLinePressure, 0.714);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamLinePressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamLinePressure", "15");
        Assert.AreEqual(cfg.SpamLinePressure, 15.0);
        Assert.AreEqual("I've set `SpamLinePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamPingPressure 15"
        Assert.AreEqual(cfg.SpamPingPressure, 2.5);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamPingPressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamPingPressure", "15");
        Assert.AreEqual(cfg.SpamPingPressure, 15.0);
        Assert.AreEqual("I've set `SpamPingPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamRepeatPressure 15"
        Assert.AreEqual(cfg.SpamRepeatPressure, 10.0);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamRepeatPressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamRepeatPressure", "15");
        Assert.AreEqual(cfg.SpamRepeatPressure, 15.0);
        Assert.AreEqual("I've set `SpamRepeatPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamUnusualCharacterPressure 0.05"
        Assert.AreEqual(cfg.SpamUnusualCharacterPressure, 0.01);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamUnusualCharacterPressure 0.05");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamUnusualCharacterPressure", "0.05");
        Assert.AreEqual(cfg.SpamUnusualCharacterPressure, 0.05);
        Assert.AreEqual("I've set `SpamUnusualCharacterPressure` to the following content: 0.05", generalChannel.Messages.Last().Content);

        // post ".config SpamMaxPressure 15"
        Assert.AreEqual(cfg.SpamMaxPressure, 60.0);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamMaxPressure 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamMaxPressure", "15");
        Assert.AreEqual(cfg.SpamMaxPressure, 15.0);
        Assert.AreEqual("I've set `SpamMaxPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamPressureDecay 15"
        Assert.AreEqual(cfg.SpamPressureDecay, 2.5);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamPressureDecay 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamPressureDecay", "15");
        Assert.AreEqual(cfg.SpamPressureDecay, 15.0);
        Assert.AreEqual("I've set `SpamPressureDecay` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamMessageDeleteLookback 15"
        Assert.AreEqual(cfg.SpamMessageDeleteLookback, 60);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SpamMessageDeleteLookback 15");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SpamMessageDeleteLookback", "15");
        Assert.AreEqual(cfg.SpamMessageDeleteLookback, 15.0);
        Assert.AreEqual("I've set `SpamMessageDeleteLookback` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config RaidProtectionEnabled false"
        Assert.AreEqual(cfg.RaidProtectionEnabled, true);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config RaidProtectionEnabled false");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "RaidProtectionEnabled", "false");
        Assert.AreEqual(cfg.RaidProtectionEnabled, false);
        Assert.AreEqual("I've set `RaidProtectionEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config AutoSilenceNewJoins true"
        Assert.AreEqual(cfg.AutoSilenceNewJoins, false);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config AutoSilenceNewJoins true");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "AutoSilenceNewJoins", "true");
        Assert.AreEqual(cfg.AutoSilenceNewJoins, true);
        Assert.AreEqual("I've set `AutoSilenceNewJoins` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidSize 5"
        Assert.AreEqual(cfg.SmallRaidSize, 3);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidSize 5");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidSize", "5");
        Assert.AreEqual(cfg.SmallRaidSize, 5);
        Assert.AreEqual("I've set `SmallRaidSize` to the following content: 5", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidTime 50"
        Assert.AreEqual(cfg.SmallRaidTime, 180);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidTime 50");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidTime", "50");
        Assert.AreEqual(cfg.SmallRaidTime, 50);
        Assert.AreEqual("I've set `SmallRaidTime` to the following content: 50", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidSize 20"
        Assert.AreEqual(cfg.LargeRaidSize, 10);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidSize 20");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidSize", "20");
        Assert.AreEqual(cfg.LargeRaidSize, 20);
        Assert.AreEqual("I've set `LargeRaidSize` to the following content: 20", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidTime 200"
        Assert.AreEqual(cfg.LargeRaidTime, 120);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidTime 200");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidTime", "200");
        Assert.AreEqual(cfg.LargeRaidTime, 200);
        Assert.AreEqual("I've set `LargeRaidTime` to the following content: 200", generalChannel.Messages.Last().Content);

        // post ".config RecentJoinDecay 100"
        Assert.AreEqual(cfg.RecentJoinDecay, 300);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config RecentJoinDecay 100");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "RecentJoinDecay", "100");
        Assert.AreEqual(cfg.RecentJoinDecay, 100);
        Assert.AreEqual("I've set `RecentJoinDecay` to the following content: 100", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidDecay 10"
        Assert.AreEqual(cfg.SmallRaidDecay, 5);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidDecay 10");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidDecay", "10");
        Assert.AreEqual(cfg.SmallRaidDecay, 10);
        Assert.AreEqual("I've set `SmallRaidDecay` to the following content: 10", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidDecay 50"
        Assert.AreEqual(cfg.LargeRaidDecay, 30);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidDecay 50");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidDecay", "50");
        Assert.AreEqual(cfg.LargeRaidDecay, 50);
        Assert.AreEqual("I've set `LargeRaidDecay` to the following content: 50", generalChannel.Messages.Last().Content);


        // Ensure we can't forget to keep this test up to date
        var configPropsCount = typeof(Config).GetProperties().Length;

        Assert.AreEqual(50, configPropsCount,
            $"\nIf you just added or removed a config item, then this test is probably out of date");

        Assert.AreEqual(configPropsCount * 2, generalChannel.Messages.Count(),
            $"\nIf you just added or removed a config item, then this test is probably out of date");
    }

    [TestMethod()]
    public async Task ConfigCommand_SuggestionTestsAsync()
    {
        var (cfg, cd, (izzyHerself, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        // mis-capitalization

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config prefix");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "prefix", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `prefix`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        // missing letters

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Prefi");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Prefi", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `Prefi`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Pref");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Pref", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `Pref`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Pre");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Pre", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `Pre`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        // extra letters

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config Prefixes");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "Prefixes", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `Prefixes`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        // multiple kinds of typo at once

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config prefz");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "prefz", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `prefz`!" +
            "\nDid you mean `Prefix`?",
            generalChannel.Messages.Last().Content);

        // with additional arguments

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config alias set moonlaser addquote moon");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "alias", "set moonlaser addquote moon");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `alias`!" +
            "\nDid you mean `Aliases`?",
            generalChannel.Messages.Last().Content);

        // many suggestions

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".config spampressure");
        await ConfigCommand.TestableConfigCommandAsync(context, cfg, cd, "spampressure", "");

        Assert.AreEqual("Sorry, I couldn't find a config value or category called `spampressure`!" +
            "\nDid you mean `SpamBasePressure` or `SpamImagePressure` or `SpamLengthPressure` or `SpamLinePressure` or `SpamPingPressure` or `SpamRepeatPressure` or `SpamMaxPressure` or `SpamPressureDecay`?",
            generalChannel.Messages.Last().Content);
    }
}
