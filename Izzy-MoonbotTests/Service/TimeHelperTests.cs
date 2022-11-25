using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_MoonbotTests.Helpers;

[TestClass()]
public class TimeHelperTests
{
    [TestMethod()]
    public void GetTimeTypeTests()
    {
        Assert.AreEqual("unknown", TimeHelper.GetTimeType(""));
        Assert.AreEqual("absolute date", TimeHelper.GetTimeType("on friday at 03:00"));
        Assert.AreEqual("absolute time", TimeHelper.GetTimeType("at 10 am"));
        Assert.AreEqual("relative", TimeHelper.GetTimeType("in 10 minutes"));
        Assert.AreEqual("repeat", TimeHelper.GetTimeType("every january"));
    }

    void AssertTimeHelperResponsesAreWithinOneSecond(TimeHelperResponse expected, TimeHelperResponse actual)
    {
        Assert.AreEqual(expected.Repeats, actual.Repeats, "\nRepeats");
        Assert.AreEqual(expected.RepeatType, actual.RepeatType, "\nRepeatType");
        Assert.AreEqual(
            expected.Time.Ticks - (expected.Time.Ticks % TimeSpan.TicksPerSecond),
            actual.Time.Ticks - (actual.Time.Ticks % TimeSpan.TicksPerSecond),
            "\nTime"
        );
    }

    [TestMethod()]
    public void Convert_RelativeTests()
    {
        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddMinutes(10), false, null),
            TimeHelper.Convert("in 10 minutes")
        );

        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddHours(1), false, null),
            TimeHelper.Convert("in 1 hour")
        );

        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddSeconds(37), false, null),
            TimeHelper.Convert("in 37 seconds")
        );

        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddDays(7), false, null),
            TimeHelper.Convert("in 7 days")
        );

        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddMonths(6), false, null),
            TimeHelper.Convert("in 6 months")
        );
    }

    [TestMethod()]
    public void Convert_MiscTests()
    {
        Assert.ThrowsException<FormatException>(() => TimeHelper.Convert(""));

        var now = DateTimeOffset.UtcNow;
        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(new DateTimeOffset(now.Year, now.Month, now.Day, 3, 15, 0, 0, TimeSpan.Zero), false, null),
            TimeHelper.Convert("at 03:15")
        );

        now = DateTimeOffset.UtcNow;
        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(new DateTimeOffset(now.Year, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), false, null),
            TimeHelper.Convert("on 1st jan at 12:00")
        );

        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(DateTimeOffset.UtcNow.AddHours(1), true, "relative"),
            TimeHelper.Convert("every hour")
        );

        now = DateTimeOffset.UtcNow;
        AssertTimeHelperResponsesAreWithinOneSecond(
            new TimeHelperResponse(new DateTimeOffset(now.Year, now.Month, now.Day, 12, 30, 0, 0, TimeSpan.Zero), true, "daily"),
            TimeHelper.Convert("every day at 12:30")
        );
    }
}
