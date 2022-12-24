using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot;
using Izzy_Moonbot_Tests.Services;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class MiscModuleTests
{
    [TestMethod()]
    public async Task RemindMe_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;

        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        var mm = new MiscModule(cfg, ss, logger);

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
    public async Task Rule_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;

        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);
        var mm = new MiscModule(cfg, ss, logger);

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
}