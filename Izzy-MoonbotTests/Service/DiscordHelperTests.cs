using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_MoonbotTests.Service;

[TestClass()]
public class DiscordHelperTests
{
    [TestMethod()]
    public void MiscTests()
    {
        Assert.IsTrue(DiscordHelper.IsSpace(' '));
        Assert.IsFalse(DiscordHelper.IsSpace('a'));
    }

    [TestMethod()]
    public void StripQuotesTests()
    {
        Assert.AreEqual("", DiscordHelper.StripQuotes(""));
        Assert.AreEqual("a", DiscordHelper.StripQuotes("a"));
        Assert.AreEqual("ab", DiscordHelper.StripQuotes("ab"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("foo"));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("foo bar"));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("foo \"bar\" baz"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("\"foo\""));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("\"foo bar\""));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("\"foo \"bar\" baz\""));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo'"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("ʺfooʺ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("˝fooˮ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("“foo”"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo”"));
    }

    [TestMethod()]
    public void ConvertPingsTests()
    {
        Assert.AreEqual(0ul, DiscordHelper.ConvertChannelPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("<#1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("<#>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("foo <#1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertUserPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("<@1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("<@>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("foo <@1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertRolePingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("<@&1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("<@&>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("foo <@&1234> bar"));
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual have error messages so bad it was worth writing my own asserts
    void AssertListsAreEqual<T>(IList<T> expected, IList<T> actual, string message = "")
    {
        if (expected.Count() != actual.Count())
            Assert.AreEqual(expected, actual, $"\nCount() mismatch: {expected.Count()} != {actual.Count()}");
        foreach (var i in Enumerable.Range(0, expected.Count()))
            Assert.AreEqual(expected[i], actual[i], $"\nItem {i}" + message);
    }

    void AssertArgumentResultsAreEqual(ArgumentResult expected, ArgumentResult actual)
    {
        AssertListsAreEqual(expected.Arguments, actual.Arguments, "\nArguments");
        AssertListsAreEqual(expected.Indices, actual.Indices, "\nIndices");
    }

    string SkippedArgsString(string argsString, int argsToSkip)
    {
        var args = DiscordHelper.GetArguments(argsString);
        return string.Join("", argsString.Skip(args.Indices[argsToSkip]));
    }

    [TestMethod()]
    public void GetArguments_NoQuotesTests()
    {
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = Array.Empty<string>(), Indices = Array.Empty<int>() }, DiscordHelper.GetArguments(""));

        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = Array.Empty<string>(), Indices = Array.Empty<int>() }, DiscordHelper.GetArguments(" "));

        var argsString = "foo";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo ";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("", SkippedArgsString(argsString, 0));

        argsString = "foo bar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 8 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo    bar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 8 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("   bar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("ar", SkippedArgsString(argsString, 1)); // TODO: Incorrect

        argsString = "foo bar   ";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 8 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar   ", SkippedArgsString(argsString, 0));
        Assert.AreEqual("  ", SkippedArgsString(argsString, 1));

        argsString = "foo baaaar";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "baaaar" }, Indices = new[] { 4, 11 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("baaaar", SkippedArgsString(argsString, 0));
        Assert.AreEqual("", SkippedArgsString(argsString, 1));

        argsString = "foo bar baz";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar", "baz" }, Indices = new[] { 4, 8, 12 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("bar baz", SkippedArgsString(argsString, 0));
        Assert.AreEqual("baz", SkippedArgsString(argsString, 1));
        Assert.AreEqual("", SkippedArgsString(argsString, 2));

        argsString = "foo   bar   baz";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar", "baz" }, Indices = new[] { 4, 8, 12 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("  bar   baz", SkippedArgsString(argsString, 0));
        Assert.AreEqual("r   baz", SkippedArgsString(argsString, 1)); // TODO: Incorrect
        Assert.AreEqual("baz", SkippedArgsString(argsString, 2)); // TODO: Incorrect
    }

    [TestMethod()]
    public void GetArguments_QuotesTests()
    {
        var argsString = "\"\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "" }, Indices = new[] { 1 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"", SkippedArgsString(argsString, 0)); // TODO: Incorrect

        argsString = "\"foo\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo" }, Indices = new[] { 4 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"", SkippedArgsString(argsString, 0)); // TODO: Incorrect

        argsString = "\"foo bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo bar" }, Indices = new[] { 8 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"", SkippedArgsString(argsString, 0)); // TODO: Incorrect

        argsString = "foo \"bar\"";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar" }, Indices = new[] { 4, 8 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar\"", SkippedArgsString(argsString, 0));
        Assert.AreEqual("\"", SkippedArgsString(argsString, 1)); // TODO: Incorrect

        argsString = "foo \"bar baz\" quux";
        AssertArgumentResultsAreEqual(new ArgumentResult{ Arguments = new[] { "foo", "bar baz", "quux" }, Indices = new[] { 4, 12, 17 } }, DiscordHelper.GetArguments(argsString));
        Assert.AreEqual("\"bar baz\" quux", SkippedArgsString(argsString, 0));
        Assert.AreEqual("\" quux", SkippedArgsString(argsString, 1)); // TODO: Incorrect
        Assert.AreEqual("x", SkippedArgsString(argsString, 2)); // TODO: Incorrect
    }

    public TestIzzyContext DefaultTestContext()
    {
        // Unrealistic assumptions made here include but are not limited to:
        // - There is only one guild/server the bot ever sees
        // - All users are present in all channels

        var izzyHerself = new TestUser("Izzy Moonbot", 1);
        var users = new List<TestUser>{
            izzyHerself,
            new TestUser("Sunny", 2)
        };

        var roles = new List<TestRole> {
            new TestRole("Alicorn", 1)
        };
        var channels = new List<TestTextChannel>{
            new TestTextChannel("general", 1, () => users)
        };

        var guild = new TestGuild(1, users, channels, roles);
        var client = new TestClient(izzyHerself, new[] { guild });

        Func<ulong, Task<IIzzyUser>> userGetter = (ulong id) => Task.FromResult((IIzzyUser)users.Where(user => user.Id == id).Single());
        return new TestIzzyContext(false, guild, client, new TestMessageChannel("general", 1, userGetter), null);
    }

    [TestMethod()]
    public async Task UserRoleChannel_GettersTests()
    {
        var context = DefaultTestContext();

        Assert.AreEqual(1ul, await GetChannelIdIfAccessAsync("1", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("999", context));

        Assert.AreEqual(1ul, await GetChannelIdIfAccessAsync("<#1>", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("<#999>", context));

        Assert.AreEqual(1ul, await GetChannelIdIfAccessAsync("general", context));
        Assert.AreEqual(0ul, await GetChannelIdIfAccessAsync("other", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("1", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("999", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("<@&1>", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("<@&999>", context));

        Assert.AreEqual(1ul, GetRoleIdIfAccessAsync("Alicorn", context));
        Assert.AreEqual(0ul, GetRoleIdIfAccessAsync("other", context));

        // unlike the channel and role getters, this user method intentionally supports "unknown" users not in the guild
        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("1", context));
        Assert.AreEqual(999ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("999", context));

        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("<@1>", context));
        Assert.AreEqual(999ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("<@999>", context));

        Assert.AreEqual(1ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("Izzy", context));
        Assert.AreEqual(2ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("Sunny", context));
        Assert.AreEqual(0ul, await GetUserIdFromPingOrIfOnlySearchResultAsync("other", context));
    }
}
