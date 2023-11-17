using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class ParseHelperTests
{
    [TestMethod()]
    public void TryParseUnambiguousUser()
    {
        string? err;

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousUser("", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousUser("1234", out err));
        Assert.IsNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousUser("<@1234>", out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousUser("<@>", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousUser("foo <@1234> bar", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, "foo bar"), ParseHelper.TryParseUnambiguousUser("<@1234> foo bar", out err));
        Assert.IsNull(err);
    }

    [TestMethod()]
    public void TryParseRoleMention()
    {
        string? err;

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousRole("", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousRole("1234", out err));
        Assert.IsNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousRole("<@&1234>", out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousRole("<@&>", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousRole("foo <@&1234> bar", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, "foo bar"), ParseHelper.TryParseUnambiguousRole("<@&1234> foo bar", out err));
        Assert.IsNull(err);
    }

    [TestMethod()]
    public void TryParseChannelMention()
    {
        string? err;

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousChannel("", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousChannel("1234", out err));
        Assert.IsNull(err);

        Assert.AreEqual((1234ul, ""), ParseHelper.TryParseUnambiguousChannel("<#1234>", out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousChannel("<#>", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseUnambiguousChannel("foo <#1234> bar", out err));
        Assert.IsNotNull(err);

        Assert.AreEqual((1234ul, "foo bar"), ParseHelper.TryParseUnambiguousChannel("<#1234> foo bar", out err));
        Assert.IsNull(err);
    }

    [TestMethod()]
    async public Task TryParseUserResolvable()
    {
        var (_, _, (izzyHerself, _), _, (generalChannel, _, _), stubGuild, stubClient) = TestUtils.DefaultStubs();
        var guild = new TestGuild(stubGuild, stubClient);

        Assert.AreEqual((1ul, null), await ParseHelper.TryParseUserResolvable("1", guild));
        Assert.AreEqual((999ul, null), await ParseHelper.TryParseUserResolvable("999", guild));

        Assert.AreEqual((1ul, null), await ParseHelper.TryParseUserResolvable("<@1>", guild));
        Assert.AreEqual((999ul, null), await ParseHelper.TryParseUserResolvable("<@999>", guild));

        Assert.AreEqual((1ul, null), await ParseHelper.TryParseUserResolvable("Izzy", guild));
        Assert.AreEqual((2ul, null), await ParseHelper.TryParseUserResolvable("Sunny", guild));

        var failureResult = await ParseHelper.TryParseUserResolvable("other", guild);
        Assert.AreEqual(null, failureResult.Item1);
        StringAssert.Contains(failureResult.Item2, "0 users");
        StringAssert.Contains(failureResult.Item2, "other");
    }

    [TestMethod()]
    public void TryParseRoleResolvable()
    {
        string? err;

        var (_, _, (izzyHerself, _), _, (generalChannel, _, _), stubGuild, stubClient) = TestUtils.DefaultStubs();
        var guild = new TestGuild(stubGuild, stubClient);

        Assert.AreEqual((1ul, ""), ParseHelper.TryParseRoleResolvable("1", guild, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseRoleResolvable("999", guild, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "role with id");
        StringAssert.Contains(err, "999");

        Assert.AreEqual((1ul, ""), ParseHelper.TryParseRoleResolvable("<@&1>", guild, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseRoleResolvable("<@&999>", guild, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "role with id");
        StringAssert.Contains(err, "999");

        Assert.AreEqual((1ul, ""), ParseHelper.TryParseRoleResolvable("Alicorn", guild, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseRoleResolvable("other", guild, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "role with name");
        StringAssert.Contains(err, "other");
    }

    [TestMethod()]
    async public Task TryParseChannelResolvable()
    {
        string? err;

        var (_, _, (izzyHerself, _), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, izzyHerself.Id, "hello");

        Assert.AreEqual((generalChannel.Id, ""), ParseHelper.TryParseChannelResolvable($"{generalChannel.Id}", context, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseChannelResolvable("999", context, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "channel with id");
        StringAssert.Contains(err, "999");

        Assert.AreEqual((generalChannel.Id, ""), ParseHelper.TryParseChannelResolvable($"<#{generalChannel.Id}>", context, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseChannelResolvable("<#999>", context, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "channel with id");
        StringAssert.Contains(err, "999");

        Assert.AreEqual((generalChannel.Id, ""), ParseHelper.TryParseChannelResolvable("general", context, out err));
        Assert.IsNull(err);

        Assert.AreEqual(null, ParseHelper.TryParseChannelResolvable("other", context, out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "channel with name");
        StringAssert.Contains(err, "other");
    }

    public static void AssertParseDateTimeResultAreEqual(ParseDateTimeResult expected, ParseDateTimeResult actual)
    {
        Assert.AreEqual(expected.RepeatType, actual.RepeatType, "\nRepeatType");
        Assert.AreEqual(expected.Time, actual.Time, "\nTime");
    }

    public static void AssertTryParseDateTime(
        (ParseDateTimeResult, string)? actualResponse,
        DateTimeOffset expectedTime,
        ScheduledJobRepeatType expectedRepeatType,
        string expectedRemainingArgsString)
    {
        Assert.IsNotNull(actualResponse);
        if (actualResponse is var (thr, remainingArgsString))
        {
            AssertParseDateTimeResultAreEqual(
                new ParseDateTimeResult(expectedTime, expectedRepeatType),
                thr
            );
            Assert.AreEqual(expectedRemainingArgsString, remainingArgsString);
        }
    }

    [TestMethod()]
    public void TryParseDateTime_IntervalTests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 10 minutes", out err),
            DateTimeHelper.UtcNow.AddMinutes(10), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 1 hour", out err),
            DateTimeHelper.UtcNow.AddHours(1), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 37 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(37), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 7 days", out err),
            DateTimeHelper.UtcNow.AddDays(7), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 6 months", out err),
            DateTimeHelper.UtcNow.AddMonths(6), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);


        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 1 hour here's some text", out err),
            DateTimeHelper.UtcNow.AddHours(1), ScheduledJobRepeatType.None, "here's some text"
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("1 hour here's some text", out err),
            DateTimeHelper.UtcNow.AddHours(1), ScheduledJobRepeatType.None, "here's some text"
        );
        Assert.AreEqual(err, null);


        Assert.IsNull(ParseHelper.TryParseDateTime("in one hour", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"one\"");
        StringAssert.Contains(err, "not a positive integer");

        Assert.IsNull(ParseHelper.TryParseDateTime("one hour", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"one\"");
        StringAssert.Contains(err, "extract a weekday + time"); // without the 'in', this gets mistaken for an attempted weekday

        Assert.IsNull(ParseHelper.TryParseDateTime("in 1 xyz", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"xyz\"");
        StringAssert.Contains(err, "interval units:");

        Assert.IsNull(ParseHelper.TryParseDateTime("1 xyz", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"xyz\"");
        StringAssert.Contains(err, "interval units:");
    }

    [TestMethod()]
    public void TryParseDateTime_MiscError_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        Assert.IsNull(ParseHelper.TryParseDateTime("", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"\" can't be a datetime");
    }

    [TestMethod()]
    public void TryParseDateTime_MultipleDigitsTests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 123 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(123), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 1234 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(1234), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("in 12345 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(12345), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_DiscordTimestamp_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("<t:123456789>", out err),
            DateTimeOffset.FromUnixTimeSeconds(123456789), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("<t:123456789:R>", out err),
            DateTimeOffset.FromUnixTimeSeconds(123456789), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("<t:0:x>", out err),
            DateTimeOffset.FromUnixTimeSeconds(0), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(ParseHelper.TryParseDateTime("123456789", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"123456789\"");
        StringAssert.Contains(err, "not a Discord timestamp");
    }

    [TestMethod()]
    public void TryParseDateTime_Time_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("at 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 3, 15, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 3, 15, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(ParseHelper.TryParseDateTime("at 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(ParseHelper.TryParseDateTime("03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");
    }

    [TestMethod()]
    public void TryParseDateTime_WeekdayTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        // Oct 10th 2010 was a Sunday, so "next Monday" is the 11th
        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on monday 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 3, 15, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("monday 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 3, 15, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(ParseHelper.TryParseDateTime("on monday 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday 03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(ParseHelper.TryParseDateTime("monday 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday 03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(ParseHelper.TryParseDateTime("on monday", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday\"");
        StringAssert.Contains(err, "missing a time");

        Assert.IsNull(ParseHelper.TryParseDateTime("monday", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday\"");
        StringAssert.Contains(err, "missing a time");

        // When using today's weekday and a time, the user could mean today or a week from today,
        // so it matters whether the time is in the future
        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on sunday 00:00 UTC+0", out err),
            new DateTimeOffset(2010, 10, 17, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on sunday 00:01 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 0, 1, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_DateTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on 1 jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("1 jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(ParseHelper.TryParseDateTime("on 1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"1 jan 2020 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(ParseHelper.TryParseDateTime("1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"1 jan 2020 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");

        // we support "st"/"nd"/"rd"/"th" suffixes without advertising them
        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("1st jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        // regression test that two-digit days also work
        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on 15 jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 15, 12, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("on 15th jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 15, 12, 0, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.None, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingInterval_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every 1 hour", out err),
            DateTimeHelper.UtcNow.AddHours(1), ScheduledJobRepeatType.Relative, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 12, 30, 0, TimeSpan.Zero), ScheduledJobRepeatType.Daily, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every day 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 12, 30, 0, TimeSpan.Zero), ScheduledJobRepeatType.Daily, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingWeekdayTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        // Oct 10th 2010 was a Sunday, so "next Monday" is the 11th
        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every monday 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 12, 30, 0, TimeSpan.Zero), ScheduledJobRepeatType.Weekly, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every week monday 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 12, 30, 0, TimeSpan.Zero), ScheduledJobRepeatType.Weekly, ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingDateTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every 1 jan 12:00 UTC+0", out err),
            new DateTimeOffset(2011, 1, 1, 12, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.Yearly, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            ParseHelper.TryParseDateTime("every year 1 jan 12:00 UTC+0", out err),
            new DateTimeOffset(2011, 1, 1, 12, 0, 0, TimeSpan.Zero), ScheduledJobRepeatType.Yearly, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(ParseHelper.TryParseDateTime("every 1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"every 1 jan 2020 12:00\"");
        StringAssert.Contains(err, "\"2020\" is not a valid time");

        Assert.IsNull(ParseHelper.TryParseDateTime("every 1 jan 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"every 1 jan 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");
    }

    [TestMethod()]
    public void TryParseInterval_InThePast_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        var response = ParseHelper.TryParseInterval("10 minutes", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddMinutes(-10), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = ParseHelper.TryParseInterval("1 hour", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddHours(-1), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = ParseHelper.TryParseInterval("37 seconds", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddSeconds(-37), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = ParseHelper.TryParseInterval("7 days", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddDays(-7), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = ParseHelper.TryParseInterval("6 months", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddMonths(-6), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);
    }
}
