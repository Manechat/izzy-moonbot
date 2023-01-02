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
        return (ss, new ModMiscModule(cfg, users, ss, logger));
    }

    [TestMethod()]
    public async Task Echo_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mmm) = SetupModMiscModule(cfg);

        // .echo with no channel argument

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".echo test");
        await mmm.TestableEchoCommandAsync(context, "test");

        Assert.AreEqual(2, generalChannel.Messages.Count);
        Assert.AreEqual("test", generalChannel.Messages.Last().Content);
        Assert.AreEqual(0, modChat.Messages.Count);

        // .echo'ing to another channel

        context = await client.AddMessageAsync(guild.Id, modChat.Id, sunny.Id, $".echo <#{generalChannel.Id}> hello from mod chat");
        await mmm.TestableEchoCommandAsync(context, $"<#{generalChannel.Id}> hello from mod chat");

        Assert.AreEqual(3, generalChannel.Messages.Count);
        Assert.AreEqual("hello from mod chat", generalChannel.Messages.Last().Content);
        Assert.AreEqual(1, modChat.Messages.Count);
    }

    [TestMethod()]
    public async Task Schedule_Command_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mmm) = SetupModMiscModule(cfg);

        var logger = new LoggingService(new TestLogger<Worker>());
        var cfgDescriber = new ConfigDescriber();
        var mm = new MiscModule(cfg, cfgDescriber, ss, logger, await MiscModuleTests.SetupCommandService());


        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule");
        await mmm.TestableScheduleCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of subcommands");
        StringAssert.Contains(description, "`.schedule list [jobtype]` -");
        StringAssert.Contains(description, "`.schedule about <id>` -");
        StringAssert.Contains(description, "`.schedule add");


        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule list");
        await mmm.TestableScheduleCommandAsync(context, "list");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of all the scheduled jobs");
        StringAssert.Contains(description, "\n\n\n"); // a blank list because nothing is scheduled
        StringAssert.Contains(description, "If you need a raw text file");
        Assert.AreEqual(0, ss.GetScheduledJobs().Count());


        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme 10 minutes this is a test");
        await mm.TestableRemindMeCommandAsync(context, "10 minutes this is a test");

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".schedule list");
        await mmm.TestableScheduleCommandAsync(context, "list");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's a list of all the scheduled jobs");
        StringAssert.Contains(description, $": Send \"this is a test\" to (<#{sunny.Id}>/<@{sunny.Id}>) (`{sunny.Id}`) <t:1286669400:R>.");
        StringAssert.Contains(description, "If you need a raw text file");
        Assert.AreEqual(1, ss.GetScheduledJobs().Count());
        var job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch, job.CreatedAt);
        Assert.AreEqual(TestUtils.FiMEpoch.AddMinutes(10), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobRepeatType.None, job.RepeatType);
        Assert.AreEqual(ScheduledJobActionType.Echo, job.Action.Type);
        Assert.AreEqual(sunny.Id, (job.Action as ScheduledEchoJob)?.ChannelOrUser);
        Assert.AreEqual("this is a test", (job.Action as ScheduledEchoJob)?.Content);


        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add echo 1 hour {generalChannel.Id} this is another test");
        await mmm.TestableScheduleCommandAsync(context, $"add echo 1 hour {generalChannel.Id} this is another test");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Send \"this is another test\" to (<#{generalChannel.Id}>/<@{generalChannel.Id}>) (`{generalChannel.Id}`) <t:1286672400:R>");
        Assert.AreEqual(2, ss.GetScheduledJobs().Count());
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch, job.CreatedAt);
        Assert.AreEqual(TestUtils.FiMEpoch.AddHours(1), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobRepeatType.None, job.RepeatType);
        Assert.AreEqual(ScheduledJobActionType.Echo, job.Action.Type);
        Assert.AreEqual(generalChannel.Id, (job.Action as ScheduledEchoJob)?.ChannelOrUser);
        Assert.AreEqual("this is another test", (job.Action as ScheduledEchoJob)?.Content);
    }

    [TestMethod()]
    public async Task Schedule_Add_EveryActionType_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mmm) = SetupModMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        var alicornId = roles[0].Id;

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add banner 30 minutes");
        await mmm.TestableScheduleCommandAsync(context, $"add banner 30 minutes");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, "Run Banner Rotation <t:1286670600:R>");
        var job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch.AddMinutes(30), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobActionType.BannerRotation, job.Action.Type);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add unban 6 months {sunny.Id}");
        await mmm.TestableScheduleCommandAsync(context, $"add unban 6 months {sunny.Id}");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Unban <@{sunny.Id}> (`{sunny.Id}`) <t:1302393600:R>.");
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch.AddMonths(6), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobActionType.Unban, job.Action.Type);
        Assert.AreEqual(sunny.Id, (job.Action as ScheduledUnbanJob)?.User);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add add-role 1 hour {alicornId} {sunny.Id}");
        await mmm.TestableScheduleCommandAsync(context, $"add add-role 1 hour {alicornId} {sunny.Id}");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Add <@&{alicornId}> (`{alicornId}`) to <@{sunny.Id}> (`{sunny.Id}`) <t:1286672400:R>.");
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch.AddHours(1), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobActionType.AddRole, job.Action.Type);
        Assert.AreEqual(alicornId, (job.Action as ScheduledRoleAdditionJob)?.Role);
        Assert.AreEqual(sunny.Id, (job.Action as ScheduledRoleAdditionJob)?.User);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add remove-role 25 hours {alicornId} {sunny.Id}");
        await mmm.TestableScheduleCommandAsync(context, $"add remove-role 25 hours {alicornId} {sunny.Id}");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Remove <@&{alicornId}> (`{alicornId}`) from <@{sunny.Id}> (`{sunny.Id}`) <t:1286758800:R>.");
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch.AddHours(25), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobActionType.RemoveRole, job.Action.Type);
        Assert.AreEqual(alicornId, (job.Action as ScheduledRoleRemovalJob)?.Role);
        Assert.AreEqual(sunny.Id, (job.Action as ScheduledRoleRemovalJob)?.User);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add echo 1 hour {generalChannel.Id} hello there");
        await mmm.TestableScheduleCommandAsync(context, $"add echo 1 hour {generalChannel.Id} hello there");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Send \"hello there\" to (<#{generalChannel.Id}>/<@{generalChannel.Id}>) (`{generalChannel.Id}`) <t:1286672400:R>");
        job = ss.GetScheduledJobs().Last();
        Assert.AreEqual(TestUtils.FiMEpoch.AddHours(1), job.ExecuteAt);
        Assert.AreEqual(ScheduledJobActionType.Echo, job.Action.Type);
        Assert.AreEqual(generalChannel.Id, (job.Action as ScheduledEchoJob)?.ChannelOrUser);
        Assert.AreEqual("hello there", (job.Action as ScheduledEchoJob)?.Content);
    }

    [TestMethod()]
    public async Task Schedule_RepeatingJob_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mmm) = SetupModMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        await ss.Unicycle(client);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule add echo every 10 seconds {generalChannel.Id} do the pony ony ony");
        await mmm.TestableScheduleCommandAsync(context, $"add echo every 10 seconds {generalChannel.Id} do the pony ony ony");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Created scheduled job:");
        StringAssert.Contains(description, $"Send \"do the pony ony ony\" to (<#{generalChannel.Id}>/<@{generalChannel.Id}>) (`{generalChannel.Id}`) <t:1286668810:R>, repeating Relative.");
        Assert.AreEqual(1, ss.GetScheduledJobs().Count());
        Assert.AreEqual(2, generalChannel.Messages.Count);

        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);

        Assert.AreEqual(5, generalChannel.Messages.Count);
        Assert.AreEqual("do the pony ony ony", generalChannel.Messages[2].Content);
        Assert.AreEqual("do the pony ony ony", generalChannel.Messages[3].Content);
        Assert.AreEqual("do the pony ony ony", generalChannel.Messages[4].Content);

        Assert.AreEqual(1, ss.GetScheduledJobs().Count());
        var jobId = ss.GetScheduledJobs().Single().Id;

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".schedule remove {jobId}");
        await mmm.TestableScheduleCommandAsync(context, $"remove {jobId}");

        Assert.AreEqual("Successfully deleted scheduled job.", generalChannel.Messages.Last().Content);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count());
        Assert.AreEqual(7, generalChannel.Messages.Count);

        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);
        await ss.Unicycle(client);
        Assert.AreEqual(7, generalChannel.Messages.Count);
    }

    [TestMethod()]
    public async Task Remind_Command_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mmm) = SetupModMiscModule(cfg);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        await ss.Unicycle(client);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remind");
        await mmm.TestableRemindCommandAsync(context, "");
        Assert.AreEqual("Remind you of what now? (see `.help remind`)", generalChannel.Messages.Last().Content);
        Assert.AreEqual(2, generalChannel.Messages.Count);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".remind <#{generalChannel.Id}> 1 minute test");
        await mmm.TestableRemindCommandAsync(context, $"<#{generalChannel.Id}> 1 minute test");
        Assert.AreEqual($"Okay! I'll send that reminder to <#{generalChannel.Id}> <t:1286668860:R>.", generalChannel.Messages.Last().Content);
        Assert.AreEqual(4, generalChannel.Messages.Count);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);

        await ss.Unicycle(client);

        Assert.AreEqual(5, generalChannel.Messages.Count);
        Assert.AreEqual("test", generalChannel.Messages.Last().Content);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }
}