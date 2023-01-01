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
public class ModCoreModuleTests
{
    public (ScheduleService, ModCoreModule) SetupModCoreModule(Config cfg)
    {
        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        var users = new Dictionary<ulong, User>();
        var cfgDescriber = new ConfigDescriber();
        return (ss, new ModCoreModule(logger, cfg, users, ss, mod, cfgDescriber));
    }

    [TestMethod()]
    public async Task Ban_Command_Tests()
    {
        var (cfg, _, (izzy, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mcm) = SetupModCoreModule(cfg);

        var pippId = guild.Users[3].Id;
        var hitchId = guild.Users[4].Id;

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);

        // .ban with no duration

        await client.AddMessageAsync(guild.Id, generalChannel.Id, hitchId, "anypony can make smoothies");
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban {hitchId}");
        await mcm.TestableBanCommandAsync(context, $"{hitchId}");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "I've banned Hitch (5)");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId }, guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        // .ban with duration

        await client.AddMessageAsync(guild.Id, generalChannel.Id, pippId, "which one of you broke my phone!?");
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban <@{pippId}> 5 minutes");
        await mcm.TestableBanCommandAsync(context, $"<@{pippId}> 5 minutes");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "I've banned Pipp (4)");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId, pippId }, guild.BannedUserIds);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        // changing an existing ban from indefinite to finite

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban {hitchId} 10 minutes");
        await mcm.TestableBanCommandAsync(context, $"{hitchId} 10 minutes");

        StringAssert.Contains(generalChannel.Messages.Last().Content, "This user is already banned. I have scheduled an unban");
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId, pippId }, guild.BannedUserIds);
        Assert.AreEqual(2, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual(1, modChat.Messages.Last().Embeds.Count);
        Assert.AreEqual("Unbanned Pipp#1234 (4)", modChat.Messages.Last().Embeds.Last().Title);
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { hitchId }, guild.BannedUserIds);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual(1, modChat.Messages.Last().Embeds.Count);
        Assert.AreEqual("Unbanned Hitch#1234 (5)", modChat.Messages.Last().Embeds.Last().Title);
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        // .ban myself easter egg

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".ban <@{izzy.Id}> 5 minutes");
        await mcm.TestableBanCommandAsync(context, $"<@{izzy.Id}> 5 minutes");

        // randomly selected emoji, but no actual banning
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }

    [TestMethod()]
    public async Task AssignRole_Command_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mcm) = SetupModCoreModule(cfg);

        var alicornId = roles[0].Id;
        var pippId = guild.Users[3].Id;
        var hitchId = guild.Users[4].Id;

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        Assert.IsFalse(guild.UserRoles.ContainsKey(pippId));
        Assert.IsFalse(guild.UserRoles.ContainsKey(hitchId));

        // .assignrole with no duration

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".assignrole <@&{alicornId}> {hitchId}");
        await mcm.TestableAssignRoleCommandAsync(context, $"<@&{alicornId}> {hitchId}");

        Assert.AreEqual(generalChannel.Messages.Last().Content, $"I've given <@&{alicornId}> to <@{hitchId}>.");
        Assert.IsFalse(guild.UserRoles.ContainsKey(pippId));
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[hitchId]);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        // .assignrole with duration

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".assignrole <@&{alicornId}> <@{pippId}> 5 minutes");
        await mcm.TestableAssignRoleCommandAsync(context, $"<@&{alicornId}> <@{pippId}> 5 minutes");

        Assert.AreEqual(generalChannel.Messages.Last().Content, $"I've given <@&{alicornId}> to <@{pippId}>. I've scheduled a removal <t:1286669100:R>.");
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[pippId]);
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[hitchId]);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        // changing an existing role assignment from indefinite to finite

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".assignrole <@&{alicornId}> {hitchId} 10 minutes");
        await mcm.TestableAssignRoleCommandAsync(context, $"<@&{alicornId}> {hitchId} 10 minutes");

        StringAssert.Contains(generalChannel.Messages.Last().Content, $"<@{hitchId}> already has that role. I've scheduled a removal <t:1286669400:R>.");
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[pippId]);
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[hitchId]);
        Assert.AreEqual(2, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual($"Removed <@&{alicornId}> from <@{pippId}> (`{pippId}`)", modChat.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[pippId]);
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[hitchId]);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual($"Removed <@&{alicornId}> from <@{hitchId}> (`{hitchId}`)", modChat.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[pippId]);
        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[hitchId]);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }

    [TestMethod()]
    public async Task AssignRole_ExtraSpaces_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var (ss, mcm) = SetupModCoreModule(cfg);

        var alicornId = roles[0].Id;
        var pippId = guild.Users[3].Id;

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        Assert.IsFalse(guild.UserRoles.ContainsKey(pippId));

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".assignrole <@&{alicornId}>   <@{pippId}>   5 minutes");
        await mcm.TestableAssignRoleCommandAsync(context, $"<@&{alicornId}>   <@{pippId}>   5 minutes");

        Assert.AreEqual(generalChannel.Messages.Last().Content, $"I've given <@&{alicornId}> to <@{pippId}>. I've scheduled a removal <t:1286669100:R>.");
        TestUtils.AssertListsAreEqual(new List<ulong> { alicornId }, guild.UserRoles[pippId]);
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(5);
        await ss.Unicycle(client);

        Assert.AreEqual($"Removed <@&{alicornId}> from <@{pippId}> (`{pippId}`)", modChat.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[pippId]);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }
}