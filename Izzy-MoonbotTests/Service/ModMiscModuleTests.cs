using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot;
using Izzy_Moonbot_Tests.Services;
using Discord.Commands;
using Izzy_Moonbot.Describers;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class ModMiscModuleTests
{
    public (ScheduleService, ModMiscModule) SetupModMiscModule(Config cfg)
    {
        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        var users = new Dictionary<ulong, User>();
        return (ss, new ModMiscModule(cfg, users, ss));
    }

    [TestMethod()]
    public async Task Echo_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, am) = SetupModMiscModule(cfg);

        // .echo with no channel argument

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".echo test");
        await am.TestableEchoCommandAsync(context, "test");

        Assert.AreEqual(2, generalChannel.Messages.Count);
        Assert.AreEqual("test", generalChannel.Messages.Last().Content);
        Assert.AreEqual(0, modChat.Messages.Count);

        // .echo'ing to another channel

        context = await client.AddMessageAsync(guild.Id, modChat.Id, sunny.Id, $".echo <#{generalChannel.Id}> hello from mod chat");
        await am.TestableEchoCommandAsync(context, $"<#{generalChannel.Id}> hello from mod chat");

        Assert.AreEqual(3, generalChannel.Messages.Count);
        Assert.AreEqual("hello from mod chat", generalChannel.Messages.Last().Content);
        Assert.AreEqual(1, modChat.Messages.Count);
    }

    [TestMethod()]
    public async Task Schedule_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, am) = SetupModMiscModule(cfg);

        var logger = new LoggingService(new TestLogger<Worker>());
        var cfgDescriber = new ConfigDescriber();
        var mm = new MiscModule(cfg, cfgDescriber, ss, logger, await MiscModuleTests.SetupCommandService());


        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule");
        await am.TestableScheduleCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of subcommands");
        StringAssert.Contains(description, "`.schedule list [jobtype]` -");
        StringAssert.Contains(description, "`.schedule about <id>` -");
        StringAssert.Contains(description, "`.schedule add");


        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule list");
        await am.TestableScheduleCommandAsync(context, "list");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of all the scheduled jobs!");
        StringAssert.Contains(description, "\n\n\n"); // a blank list because nothing is scheduled
        StringAssert.Contains(description, "If you need a raw text file");
        Assert.AreEqual(0, ss.GetScheduledJobs().Count());


        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme 10 minutes this is a test");
        await mm.TestableRemindMeCommandAsync(context, "10 minutes this is a test");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule list");
        await am.TestableScheduleCommandAsync(context, "list");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of all the scheduled jobs!");
        StringAssert.Contains(description, $": Send \"this is a test\" to <#{sunny.Id}> (`{sunny.Id}`) <t:1286669400:R>.");
        StringAssert.Contains(description, "If you need a raw text file");
        Assert.AreEqual(1, ss.GetScheduledJobs().Count());
        var job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch, job.CreatedAt);
        Assert.AreEqual(TestUtils.FiMEpoch.AddMinutes(10), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobRepeatType.None, job.RepeatType);
        Assert.AreEqual(ScheduledJobActionType.Echo, job.Action.Type);
        Assert.AreEqual(sunny.Id, (job.Action as ScheduledEchoJob)?.Channel);
        Assert.AreEqual("this is a test", (job.Action as ScheduledEchoJob)?.Content);


        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add echo 1 hour {generalChannel.Id} this is another test");
        await am.TestableScheduleCommandAsync(context, $"add echo 1 hour {generalChannel.Id} this is another test");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Send \"this is another test\" to <#{generalChannel.Id}> (`{generalChannel.Id}`) <t:1286672400:R>");
        Assert.AreEqual(2, ss.GetScheduledJobs().Count());
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch, job.CreatedAt);
        Assert.AreEqual(TestUtils.FiMEpoch.AddHours(1), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobRepeatType.None, job.RepeatType);
        Assert.AreEqual(ScheduledJobActionType.Echo, job.Action.Type);
        Assert.AreEqual(generalChannel.Id, (job.Action as ScheduledEchoJob)?.Channel);
        Assert.AreEqual("this is another test", (job.Action as ScheduledEchoJob)?.Content);
    }
}