﻿using Discord.Commands;
using Izzy_Moonbot;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot_Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class InfoModuleTests
{
    public async Task<CommandService> SetupCommandService()
    {
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
        services.AddHostedService<Worker>();

        var commands = new CommandService();

        // The prod code uses GetEntryAssembly() to get Izzy-Moonbot, but since Izzy-Moonbot-Tests is a different assembly,
        // we have to pick a random type from Izzy-Moonbot to get this to look over there for modules.
        await commands.AddModulesAsync(Assembly.GetAssembly(typeof(InfoModule)), services.BuildServiceProvider());

        return commands;
    }

    [TestMethod()]
    public async Task HelpCommand_BreathingTestsAsync()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var im = new InfoModule(cfg, await SetupCommandService());

        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help");
        await im.TestableHelpCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Run `.help <category>` to");
        StringAssert.Contains(description, "Run `.help <command>` to");
        StringAssert.Contains(description, "list of all the categories");
        StringAssert.Contains(description, "raid - ");
        StringAssert.Contains(description, "spam - ");
        StringAssert.Contains(description, "ℹ  **See also: `.config`");

        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help ban");
        await im.TestableHelpCommandAsync(context, "ban");

        description = generalChannel.Messages.Last().Content;
        // StringAssert.Contains is broken for strings with {}s, but explicitly passing some nulls works around that
        StringAssert.Contains(description, "**.ban** - Admin category", null, null);
        StringAssert.Contains(description, "ℹ  *This is a moderator", null, null);
        StringAssert.Contains(description, "*Bans a user", null, null);
        StringAssert.Contains(description, "Syntax: `.ban user [duration]`", null, null);
        StringAssert.Contains(description, "user [User]", null, null);
        StringAssert.Contains(description, "duration [Date/Time] {OPTIONAL}", null, null);
        StringAssert.Contains(description, "Example: ", null, null);
    }

    [TestMethod()]
    public async Task HelpCommand_Aliases_TestsAsync()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var im = new InfoModule(cfg, await SetupCommandService());

        // Check .help's regular behavior before adding aliases
        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help addquote");
        await im.TestableHelpCommandAsync(context, "addquote");

        var baseAddQuoteDescription = generalChannel.Messages.Last().Content;
        Assert.IsFalse(baseAddQuoteDescription.Contains("Relevant aliases:"));

        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help moonlaser");
        await im.TestableHelpCommandAsync(context, "moonlaser");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Sorry, I was unable to find \"moonlaser\"", null, null);

        cfg.Aliases.Add("moonlaser", "addquote moon");
        cfg.Aliases.Add("sayhi", "echo <#1> hi");
        cfg.Aliases.Add("crown", "assignrole <@1>");

        // .help should now append a Relevant Aliases line for commands with an alias
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help addquote");
        await im.TestableHelpCommandAsync(context, "addquote");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, baseAddQuoteDescription, null, null);
        StringAssert.EndsWith(description, "Relevant aliases: .moonlaser", null, null);

        // .help <alias> should now prepend the alias definition to the help for the underlying command
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help moonlaser");
        await im.TestableHelpCommandAsync(context, "moonlaser");

        description = generalChannel.Messages.Last().Content;
        StringAssert.StartsWith(description, "**.moonlaser** is an alias for **.addquote moon** (see .config Aliases)", null, null);
        StringAssert.Contains(description, baseAddQuoteDescription, null, null);

        // regression test: .help ass was mistakenly printing .assignrole's aliases because ass is a prefix of assignrole
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".help ass");
        await im.TestableHelpCommandAsync(context, "ass");

        Assert.IsFalse(generalChannel.Messages.Last().Content.Contains("Relevant aliases:"));
    }
}