using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot;
using Izzy_Moonbot_Tests.Services;
using Discord.Commands;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class AdminModuleTests
{
    public (ScheduleService, AdminModule) SetupAdminModule(Config cfg)
    {
        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        var users = new Dictionary<ulong, User>();
        var state = new State();
        return (ss, new AdminModule(logger, cfg, users, state, ss, mod));
    }

    [TestMethod()]
    public async Task Echo_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, am) = SetupAdminModule(cfg);

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
    public async Task Ban_Command_Tests()
    {
        var (cfg, _, (izzy, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, am) = SetupAdminModule(cfg);

        var pippId = guild.Users[3].Id;
        var hitchId = guild.Users[4].Id;

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);

        // .ban with no duration

        await client.AddMessageAsync(guild.Id, generalChannel.Id, hitchId, "anypony can make smoothies");
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban {hitchId}");
        await am.TestableBanCommandAsync(context, $"{hitchId}");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "I've banned Hitch (5)");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId }, guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        // .ban with duration

        await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, "which one of you broke my phone!?");
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban <@{pippId}> 5 minutes");
        await am.TestableBanCommandAsync(context, $"<@{pippId}> 5 minutes");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "I've banned Pipp (4)");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId, pippId }, guild.BannedUserIds);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        // changing an existing ban from indefinite to finite

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban {hitchId} 10 minutes");
        await am.TestableBanCommandAsync(context, $"{hitchId} 10 minutes");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "This user is already banned. I have scheduled an unban");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId, pippId }, guild.BannedUserIds);
        Assert.AreEqual(2, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual(1, modChat.Messages.Last().Embeds.Count);
        Assert.AreEqual("Unbanned Pipp#1234 <@4> (4)", modChat.Messages.Last().Embeds.Last().Title);
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId }, guild.BannedUserIds);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual(1, modChat.Messages.Last().Embeds.Count);
        Assert.AreEqual("Unbanned Hitch#1234 <@5> (5)", modChat.Messages.Last().Embeds.Last().Title);
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        // .ban myself easter egg

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban <@{izzy.Id}> 5 minutes");
        await am.TestableBanCommandAsync(context, $"<@{izzy.Id}> 5 minutes");

        // randomly selected emoji, but no actual banning
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }
}