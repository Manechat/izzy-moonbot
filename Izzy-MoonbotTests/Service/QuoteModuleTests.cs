using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using static Izzy_Moonbot.Modules.MiscModule;
using Izzy_Moonbot.Modules;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class QuoteModuleTests
{
    [TestMethod()]
    public async Task BasicTests()
    {
        var (cfg, cd, (izzy, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;

        // only one quote so that the "random" selection is deterministic for now
        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "q1" });

        var users = new Dictionary<ulong, User>();
        var s = new User(); s.Username = "Sunny Starscout"; users.Add(1, s);
        var z = new User(); z.Username = "Zephyrina Storm"; users.Add(3, z);

        var qs = new QuoteService(quotes, users);
        var qm = new QuotesSubmodule(cfg, qs);

        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".quote");
        await qm.TestableQuoteCommandAsync(context, "");

        Assert.AreEqual(2, generalChannel.Messages.Count);
        Assert.AreEqual("**Sunny `#1`:** q1", generalChannel.Messages.Last().Content);
        /*
        qs.GetKeyList(guild);
         * Quote GetQuote(IUser user, int id)
         * Quote[] GetQuotes(IUser user)
         * Quote GetRandomQuote(SocketGuild guild)
         * Quote GetRandomQuote(IUser user)
         * Task<Quote> AddQuote(IUser user, string content)
         * Task<Quote> RemoveQuote(IUser user, int id)
         */
    }
}