using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class QuoteServiceTests
{
    [TestMethod()]
    public async Task BasicTests()
    {
        var (cfg, _, (_, sunny), _, _, guild, client) = TestUtils.DefaultStubs();
        var testGuild = new TestGuild(guild, client);

        // only one quote so that the "random" selection is deterministic for now
        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "gonna be my day" });

        var users = new Dictionary<ulong, User>();
        var s = new User(); s.Username = "Sunny Starscout"; users.Add(1, s);
        var p = new User(); p.Username = "Pipp Petals"; users.Add(2, p);

        var qs = new QuoteService(quotes, users);

        Assert.AreEqual((0, "gonna be my day"), qs.GetRandomQuote(sunny.Id));

        TestUtils.AssertListsAreEqual(new List<string> { "Sunny (Sunny/2) " }, qs.GetKeyList(testGuild));

        Assert.AreEqual("gonna be my day", qs.GetQuote(sunny.Id, 0));

        TestUtils.AssertListsAreEqual(new List<string> {
            "gonna be my day"
        }, qs.GetQuotes(sunny.Id));

        await qs.AddQuote(sunny, "eat more vegetables");

        TestUtils.AssertListsAreEqual(new List<string> {
            "gonna be my day",
            "eat more vegetables"
        }, qs.GetQuotes(sunny.Id));

        await qs.RemoveQuote(sunny.Id, 0);

        TestUtils.AssertListsAreEqual(new List<string> {
            "eat more vegetables"
        }, qs.GetQuotes(sunny.Id));
    }

    [TestMethod()]
    public void QuoteAliasTests()
    {
        var quotes = new QuoteStorage();
        quotes.Aliases.Add("short", "pipp");

        var users = new Dictionary<ulong, User>();

        var qs = new QuoteService(quotes, users);

        Assert.IsTrue(qs.AliasExists("short"));
        Assert.IsFalse(qs.AliasExists("long"));

        // TODO: alias editing
        /*
         * string AliasRefersTo(string alias, IIzzyGuild guild)
         * IIzzyUser ProcessAlias(string alias, IIzzyGuild guild)
         * string ProcessAlias(string alias)
         * Task AddAlias(string alias, IIzzyUser user)
         * Task AddAlias(string alias, string category)
         * Task RemoveAlias(string alias)
         * GetAliasKeyList()
         */
    }

    // TODO: category tests
    /*
     * Quote GetQuote(string name, int id)
     * Quote[] GetQuotes(string name)
     * Quote GetRandomQuote(string name)
     * Task<Quote> AddQuote(string name, string content)
     * Task<Quote> RemoveQuote(string name, int id)
     */
}
