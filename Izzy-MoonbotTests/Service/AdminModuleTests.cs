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
public class AdminModuleTests
{
    [TestMethod()]
    public async Task Echo_Command_Tests()
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

        var users = new Dictionary<ulong, User>();
        var state = new State();
        var am = new AdminModule(logger, cfg, users, state, ss, mod);

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
}