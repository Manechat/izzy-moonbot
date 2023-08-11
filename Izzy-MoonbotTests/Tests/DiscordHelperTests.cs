using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_Moonbot_Tests.Helpers;

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
    public void GetArgument_NoQuotesTests()
    {
        Assert.AreEqual((null, null), DiscordHelper.GetArgument(""));

        Assert.AreEqual((null, null), DiscordHelper.GetArgument(" "));

        Assert.AreEqual(("foo", null), DiscordHelper.GetArgument("foo"));

        Assert.AreEqual(("foo", null), DiscordHelper.GetArgument("foo "));

        Assert.AreEqual(("foo", null), DiscordHelper.GetArgument(" foo"));

        Assert.AreEqual(("foo", "bar"), DiscordHelper.GetArgument("foo bar"));
        Assert.AreEqual(("bar", null), DiscordHelper.GetArgument("bar"));

        Assert.AreEqual(("foo", "bar"), DiscordHelper.GetArgument("foo    bar"));
        Assert.AreEqual(("bar", null), DiscordHelper.GetArgument("bar"));

        Assert.AreEqual(("foo", "bar   "), DiscordHelper.GetArgument("foo bar   "));
        Assert.AreEqual(("bar", null), DiscordHelper.GetArgument("bar   "));

        Assert.AreEqual(("foo", "bar"), DiscordHelper.GetArgument("   foo bar"));
        Assert.AreEqual(("bar", null), DiscordHelper.GetArgument("bar"));

        Assert.AreEqual(("foo", "baaaar"), DiscordHelper.GetArgument("foo baaaar"));
        Assert.AreEqual(("baaaar", null), DiscordHelper.GetArgument("baaaar"));

        Assert.AreEqual(("foo", "bar baz"), DiscordHelper.GetArgument("foo bar baz"));
        Assert.AreEqual(("bar", "baz"), DiscordHelper.GetArgument("bar baz"));
        Assert.AreEqual(("baz", null), DiscordHelper.GetArgument("baz"));

        Assert.AreEqual(("foo", "bar   baz"), DiscordHelper.GetArgument("foo   bar   baz"));
        Assert.AreEqual(("bar", "baz"), DiscordHelper.GetArgument("bar   baz"));
        Assert.AreEqual(("baz", null), DiscordHelper.GetArgument("baz"));

        Assert.AreEqual(("foo", "bar   baz   "), DiscordHelper.GetArgument("   foo   bar   baz   "));
        Assert.AreEqual(("bar", "baz   "), DiscordHelper.GetArgument("bar   baz   "));
        Assert.AreEqual(("baz", null), DiscordHelper.GetArgument("baz   "));
    }

    [TestMethod()]
    public void GetArgument_QuotesTests()
    {
        Assert.AreEqual(("", null), DiscordHelper.GetArgument("\"\""));

        Assert.AreEqual(("foo", null), DiscordHelper.GetArgument("\"foo\""));

        Assert.AreEqual(("foo bar", null), DiscordHelper.GetArgument("\"foo bar\""));

        Assert.AreEqual(("foo", "\"bar\""), DiscordHelper.GetArgument("foo \"bar\""));
        Assert.AreEqual(("bar", null), DiscordHelper.GetArgument("\"bar\""));

        Assert.AreEqual(("foo", "\"bar baz\" quux"), DiscordHelper.GetArgument("foo \"bar baz\" quux"));
        Assert.AreEqual(("bar baz", "quux"), DiscordHelper.GetArgument("\"bar baz\" quux"));
        Assert.AreEqual(("quux", null), DiscordHelper.GetArgument("quux"));
    }

    [TestMethod()]
    public void GetArgument_EscapedQuotesTests()
    {
        var parse = DiscordHelper.GetArgument("""
            "\""
            """);
        Assert.AreEqual(("""
            \"
            """, null), parse);

        parse = DiscordHelper.GetArgument("""
            "foo\"bar"
            """);
        Assert.AreEqual(("""
            foo\"bar
            """, null), parse);

        parse = DiscordHelper.GetArgument("""
            "fo\"o b\"ar"
            """);
        Assert.AreEqual(("""
            fo\"o b\"ar
            """, null), parse);

        parse = DiscordHelper.GetArgument("""
            foo\" "bar"
            """);
        Assert.AreEqual(("""
            foo\"
            """, """
            "bar"
            """), parse);
        parse = DiscordHelper.GetArgument("""
            "bar"
            """);
        Assert.AreEqual(("bar", null), parse);

        parse = DiscordHelper.GetArgument("""
            foo "bar baz\"" quux
            """);
        Assert.AreEqual(("foo", """
            "bar baz\"" quux
            """), parse);
        parse = DiscordHelper.GetArgument("""
            "bar baz\"" quux
            """);
        Assert.AreEqual(("""
            bar baz\"
            """, "quux"), parse);
        Assert.AreEqual(("quux", null), DiscordHelper.GetArgument("quux"));
    }

    [TestMethod()]
    public void TrimDiscordWhitespace_Tests()
    {
        Assert.AreEqual("", TrimDiscordWhitespace(""));
        Assert.AreEqual("", TrimDiscordWhitespace("\n"));
        Assert.AreEqual("", TrimDiscordWhitespace("\n\n\n"));
        Assert.AreEqual("", TrimDiscordWhitespace(":blank:"));
        Assert.AreEqual("", TrimDiscordWhitespace(":blank::blank::blank:"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\nIzzy\n"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy:blank:"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:Izzy:blank:"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n:blank:Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace(":blank:\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n:blank:"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy:blank:\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n:blank:Izzy\n:blank:"));

        Assert.AreEqual("IzzyIzzyIzzy", TrimDiscordWhitespace("\n:blank: \n:blank: \nIzzyIzzyIzzy\n"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy<:blank:833008517257756752>"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>Izzy<:blank:833008517257756752>"));

        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n<:blank:833008517257756752>Izzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("<:blank:833008517257756752>\nIzzy"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy\n<:blank:833008517257756752>"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("Izzy<:blank:833008517257756752>\n"));
        Assert.AreEqual("Izzy", TrimDiscordWhitespace("\n<:blank:833008517257756752>Izzy\n<:blank:833008517257756752>"));

        Assert.AreEqual("IzzyIzzyIzzy", TrimDiscordWhitespace("\n<:blank:833008517257756752> \n<:blank:833008517257756752> \nIzzyIzzyIzzy\n"));

    }

    [TestMethod()]
    public void LevenshteinDistance_Tests()
    {
        Assert.IsTrue(WithinLevenshteinDistanceOf("", "", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy", 0));

        Assert.IsFalse(WithinLevenshteinDistanceOf("", "Izzy", 0));
        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "", 0));
        Assert.IsFalse(WithinLevenshteinDistanceOf("", "Izzy", 3));
        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "", 3));
        Assert.IsTrue(WithinLevenshteinDistanceOf("", "Izzy", 4));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "", 4));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Iggy", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Iggy", 2));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Iggy", 3));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izzy!", 2));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "Izz", 0));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izz", 1));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "Izz", 2));

        Assert.IsFalse(WithinLevenshteinDistanceOf("Izzy", "izy!", 2));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "izy!", 3));
        Assert.IsTrue(WithinLevenshteinDistanceOf("Izzy", "izy!", 4));

        Assert.IsFalse(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 5));
        Assert.IsTrue(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 6));
        Assert.IsTrue(WithinLevenshteinDistanceOf("SpamMaxPressure", "SpamPressureMax", 7));
    }
}
