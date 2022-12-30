using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Izzy_Moonbot.Helpers;

namespace Izzy_Moonbot.Helpers;

public static class TimeHelper
{
    public static (TimeHelperResponse, string)? TryParseDateTime(string argsString, out string? errorString)
    {
        errorString = null;

        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a datetime";
            return null;
        }

        if (args.Arguments[0] == "in")
        {
            var intervalArgsString = string.Join("", argsString.Skip(args.Indices[0]));
            if (TryParseInterval(intervalArgsString, out errorString) is var (dto, remainingArgs))
                return (new TimeHelperResponse(dto, null), remainingArgs);
            return null; // only one possible case, so on failure we simply propagate the error and return
        }
        else if (args.Arguments[0] == "at")
        {
            var timeArgsString = string.Join("", argsString.Skip(args.Indices[0]));
            if (TryParseTimeInput(timeArgsString, out errorString) is var (dto, remainingArgs))
                return (new TimeHelperResponse(dto, null), remainingArgs);
            return null; // only one possible case, so on failure we simply propagate the error and return
        }
        else if (args.Arguments[0] == "on")
        {
            var subArgsString = string.Join("", argsString.Skip(args.Indices[0]));
            if (TryParseWeekdayTime(subArgsString, out var weekdayError) is var (weekdayDto, weekdayRemainingArgs))
                return (new TimeHelperResponse(weekdayDto, null), weekdayRemainingArgs);
            if (TryParseAbsoluteDateTime(subArgsString, out var dateError) is var (dateDto, dateRemainingArgs))
                return (new TimeHelperResponse(dateDto, null), dateRemainingArgs);

            errorString = $"Failed to extract a date/time from the start of \"{argsString}\". Using \"on\" means either weekday + time or date + time, but:\n" +
                $"Not a valid weekday + time because: {weekdayError}\n" +
                $"Not a valid date + time because: {dateError}";
            return null;
        }
        else if (args.Arguments[0] == "every")
        {
            // no disambiguation word, so we have to try every *repeatable* format, i.e.
            // no timestamps and AbsoluteDateTime are replaced by DayMonthTime
            var subArgsString = string.Join("", argsString.Skip(args.Indices[0]));
            if (TryParseInterval(subArgsString, out var intervalError) is var (intervalDto, intervalRemainingArgs))
                return (new TimeHelperResponse(intervalDto, "relative"), intervalRemainingArgs);
            if (TryParseTimeInput(subArgsString, out var timeError) is var (timeDto, timeRemainingArgs))
                return (new TimeHelperResponse(timeDto, "daily"), timeRemainingArgs);
            if (TryParseWeekdayTime(subArgsString, out var weekdayError) is var (weekdayDto, weekdayRemainingArgs))
                return (new TimeHelperResponse(weekdayDto, "weekly"), weekdayRemainingArgs);
            if (TryParseDayMonthTime(subArgsString, out var dateError) is var (dateDto, dateRemainingArgs))
                return (new TimeHelperResponse(dateDto, "yearly"), dateRemainingArgs);

            errorString = $"Failed to extract a date/time from the start of \"{argsString}\". Using \"every\" means a repeating date/time, but:\n" +
                $"Not a valid repeating interval because: {intervalError}\n" +
                $"Not a valid repeating time because: {timeError}\n" +
                $"Not a valid repeating weekday + time because: {weekdayError}\n" +
                $"Not a valid repeating date + time because: {dateError}";
            return null;
        }
        else
        {
            // no disambiguation word, so we have to try every valid format
            if (TryParseDiscordTimestamp(argsString, out var timestampError) is var (timestampDto, timestampRemainingArgs))
                return (new TimeHelperResponse(timestampDto, null), timestampRemainingArgs);
            if (TryParseInterval(argsString, out var intervalError) is var (intervalDto, intervalRemainingArgs))
                return (new TimeHelperResponse(intervalDto, null), intervalRemainingArgs);
            if (TryParseTimeInput(argsString, out var timeError) is var (timeDto, timeRemainingArgs))
                return (new TimeHelperResponse(timeDto, null), timeRemainingArgs);
            if (TryParseWeekdayTime(argsString, out var weekdayError) is var (weekdayDto, weekdayRemainingArgs))
                return (new TimeHelperResponse(weekdayDto, null), weekdayRemainingArgs);
            if (TryParseAbsoluteDateTime(argsString, out var dateError) is var (dateDto, dateRemainingArgs))
                return (new TimeHelperResponse(dateDto, null), dateRemainingArgs);

            errorString = $"Failed to extract a date/time from the start of \"{argsString}\":\n" +
                $"{timestampError}\n" + // there are no substeps for parsing a Discord timestamp, so the "...because:" will always be redundant
                $"Not a valid interval because: {intervalError}\n" +
                $"Not a valid time because: {timeError}\n" +
                $"Not a valid weekday + time because: {weekdayError}\n" +
                $"Not a valid date + time because: {dateError}";
            return null;
        }
    }

    public static (DateTimeOffset, string)? TryParseDiscordTimestamp(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a Discord timestamp";
            return null;
        }

        var match = Regex.Match(args.Arguments[0], "^<t:(?<epoch>[0-9]+)(:[a-z])?>$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var epochString = match.Groups["epoch"].Value;
            var epochSeconds = long.Parse(epochString);
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);

            errorString = null;
            return (dateTimeOffset, string.Join("", argsString.Skip(args.Indices[0])));
        }
        else
        {
            errorString = $"\"{args.Arguments[0]}\" is not a Discord timestamp (e.g. \"<t:1234567890>\", \"<t:1234567890:R>\")";
            return null;
        }
    }

    public static (DateTimeOffset, string)? TryParseInterval(string argsString, out string? errorString, bool inThePast = false)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a date/time interval";
            return null;
        }

        if (!int.TryParse(args.Arguments[0], out int dateInt))
        {
            errorString = $"\"{args.Arguments[0]}\" is not a positive integer";
            return null;
        }
        if (dateInt < 0)
        {
            errorString = $"{dateInt} is negative; only positive integers are supported";
            return null;
        }

        if (args.Arguments.Length < 2)
        {
            errorString = $"incomplete date/time interval: \"{argsString}\" contains a number but not a unit";
            return null;
        }

        var unitMatch = Regex.Match(args.Arguments[1], "^(?<unit>year|month|day|week|hour|minute|second)s?$", RegexOptions.IgnoreCase);
        if (!unitMatch.Success)
        {
            errorString = $"\"{args.Arguments[1]}\" is not one of the supported date/time interval units: year(s), month(s), day(s), week(s), hour(s), minute(s), second(s)";
            return null;
        }

        var unitString = unitMatch.Groups["unit"].Value;
        var dateTimeOffset = unitString switch
        {
            "year" => inThePast ? DateTimeHelper.UtcNow.AddYears(-dateInt) : DateTimeHelper.UtcNow.AddYears(dateInt),
            "month" => inThePast ? DateTimeHelper.UtcNow.AddMonths(-dateInt) : DateTimeHelper.UtcNow.AddMonths(dateInt),
            "week" => inThePast ? DateTimeHelper.UtcNow.AddDays(-(dateInt * 7)) : DateTimeHelper.UtcNow.AddDays(dateInt * 7),
            "day" => inThePast ? DateTimeHelper.UtcNow.AddDays(-dateInt) : DateTimeHelper.UtcNow.AddDays(dateInt),
            "hour" => inThePast ? DateTimeHelper.UtcNow.AddHours(-dateInt) : DateTimeHelper.UtcNow.AddHours(dateInt),
            "minute" => inThePast ? DateTimeHelper.UtcNow.AddMinutes(-dateInt) : DateTimeHelper.UtcNow.AddMinutes(dateInt),
            "second" => inThePast ? DateTimeHelper.UtcNow.AddSeconds(-dateInt) : DateTimeHelper.UtcNow.AddSeconds(dateInt),
            _ => throw new FormatException($"UNKNOWN_INERVAL_UNIT: {unitString}")
        };

        errorString = null;
        return (dateTimeOffset, string.Join("", argsString.Skip(args.Indices[1])));
    }

    public static (TimeSpan, string)? TryParseOffset(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a time token";
            return null;
        }

        var offsetRegex = new Regex("^UTC(?<sign>\\+|-)(?<hours>\\d\\d?)(\\:(?<minutes>\\d\\d))?$", RegexOptions.IgnoreCase);
        var match = offsetRegex.Match(args.Arguments[0]);
        if (!match.Success)
        {
            errorString = $"\"{args.Arguments[0]}\" is not a valid UTC offset (e.g. \"UTC+0\", \"UTC-8\", \"UTC+11\", \"UTC-05:30\")";
            return null;
        }

        var sign = match.Groups["sign"].Value;
        var hours = match.Groups["hours"].Value;
        var minutes = match.Groups["minutes"].Value;

        var span = new TimeSpan(
            hours == "" ? 0 : int.Parse(hours),
            minutes == "" ? 0 : int.Parse(minutes),
            0);

        if (sign == "-")
            span = TimeSpan.Zero - span;

        errorString = null;
        return (span, string.Join("", argsString.Skip(args.Indices[0])));
    }

    // this is for a single "2pm" or "17:30" token in a larger date format
    // is called by TryParseTimeInput()
    public static (int, int, string)? TryParseTimeToken(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a time token";
            return null;
        }

        var timeRegex = new Regex("^(?<hour>\\d\\d|\\d)(:(?<minute>\\d\\d))?(?<period>am|pm)?$", RegexOptions.IgnoreCase);
        var match = timeRegex.Match(args.Arguments[0]);
        if (!match.Success)
        {
            errorString = $"\"{args.Arguments[0]}\" is not a valid time (e.g. \"2pm\", \"2:30am\", \"17:15\", \"10:00pm\")";
            return null;
        }

        var hourInt = int.Parse(match.Groups["hour"].Value);

        var period = match.Groups["period"].Value;
        if (period.ToLower() == "pm" && (hourInt >= 1 && hourInt <= 11))
            hourInt += 12;
        else if (period.ToLower() == "am" && hourInt == 12)
            hourInt = 0;

        var minuteInt = 0;
        var minuteString = match.Groups["minute"].Value;
        if (minuteString != "")
            minuteInt = int.Parse(minuteString.ToLower());

        errorString = null;
        return (hourInt, minuteInt, string.Join("", argsString.Skip(args.Indices[0])));
    }

    // this is for the date/time input format that only specifies a time, e.g. "2pm UTC+0"
    // calls TryParseTimeToken()
    public static (DateTimeOffset, string)? TryParseTimeInput(string argsString, out string? errorString)
    {
        if (TryParseTimeToken(argsString, out errorString) is not var (hours, minutes, argsAfterTime))
            return null;

        if (argsAfterTime.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a UTC offset after the time (e.g. \"UTC+0\", \"UTC-8\", \"UTC+11\", \"UTC-05:30\")";
            return null;
        }

        if (TryParseOffset(argsAfterTime, out errorString) is not var (offset, argsAfterOffset))
            return null;

        var dto = new DateTimeOffset(
            DateTimeHelper.UtcNow.Year, DateTimeHelper.UtcNow.Month, DateTimeHelper.UtcNow.Day,
            hours, minutes, 0, offset);

        if (dto < DateTimeHelper.UtcNow)
            dto = dto.AddDays(1);

        errorString = null;
        return (dto, argsAfterOffset);
    }

    private static Dictionary<string, int> WeekdayNames = new() {
        { "sunday", 0 },
        { "monday", 1 },
        { "tuesday", 2 },
        { "wednesday", 3 },
        { "thursday", 4 },
        { "friday", 5 },
        { "saturday", 6 },
        { "sun", 0 },
        { "mon", 1 },
        { "tue", 2 },
        { "wed", 3 },
        { "thu", 4 },
        { "fri", 5 },
        { "sat", 6 },
    };

    public static (DateTimeOffset, string)? TryParseWeekdayTime(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a weekday + time";
            return null;
        }

        var weekdayToken = args.Arguments[0].ToLower();
        if (!WeekdayNames.Keys.Contains(weekdayToken))
        {
            errorString = $"\"{weekdayToken}\" is not one of the supported weekday names: sun(day), mon(day), tue(sday), wed(nesday), thu(rsday), fri(day), sat(urday)";
            return null;
        }
        var weekdayInt = WeekdayNames[weekdayToken];

        var argsAfterWeekday = string.Join("", argsString.Skip(args.Indices[0]));
        if (argsAfterWeekday.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a time and UTC offset after the weekday";
            return null;
        }

        if (TryParseTimeToken(argsAfterWeekday, out errorString) is not var (hours, minutes, argsAfterTime))
            return null;

        if (argsAfterTime.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a UTC offset after the time (e.g. \"UTC+0\", \"UTC-8\", \"UTC+11\", \"UTC-05:30\")";
            return null;
        }

        if (TryParseOffset(argsAfterTime, out errorString) is not var (offset, argsAfterOffset))
            return null;

        var dto = new DateTimeOffset(
            DateTimeHelper.UtcNow.Year, DateTimeHelper.UtcNow.Month, DateTimeHelper.UtcNow.Day,
            hours, minutes, 0, offset);

        // monday -> friday = 4
        // friday -> monday = -4 (-4+7 = 3)
        var daysToAdd = weekdayInt - (int)DateTimeHelper.UtcNow.DayOfWeek;
        if (daysToAdd <= 0)
            daysToAdd += 7;
        dto = dto.AddDays(daysToAdd);

        errorString = null;
        return (dto, argsAfterOffset);
    }

    public static int? TryParseDateToken(string dateToken, out string? errorString)
    {
        if (!int.TryParse(dateToken, out int dateInt))
        {
            errorString = $"\"{dateToken}\" is not a positive integer";
            return null;
        }
        if (dateInt <= 0)
        {
            errorString = $"{dateToken} is zero or negative; days are always positive";
            return null;
        }
        if (dateInt > 31)
        {
            errorString = $"{dateToken} is not a valid day because days never go higher than 31";
            return null;
        }

        errorString = null;
        return dateInt;
    }

    private static Dictionary<string, int> MonthNames = new() {
        { "january", 1 },
        { "jan", 1 },
        { "february", 2 },
        { "feb", 2 },
        { "march", 3 },
        { "mar", 3 },
        { "april", 4 },
        { "apr", 4 },
        { "may", 5 },
        { "june", 6 },
        { "jun", 6 },
        { "july", 7 },
        { "jul", 7 },
        { "august", 8 },
        { "aug", 8 },
        { "september", 9 },
        { "sep", 9 },
        { "october", 10 },
        { "oct", 10 },
        { "november", 11 },
        { "nov", 11 },
        { "december", 12 },
        { "dec", 12 },
    };

    public static int? TryParseMonthToken(string inputMonthToken, out string? errorString)
    {
        var monthToken = inputMonthToken.ToLower();
        if (!MonthNames.Keys.Contains(monthToken))
        {
            errorString = $"\"{monthToken}\" is not one of the supported month names: jan(uary), feb(ruary), mar(ch), apr(il), may, jun(e), jul(y), aug(ust), sep(tember), oct(ober), nov(ember), dec(ember)";
            return null;
        }

        errorString = null;
        return MonthNames[monthToken];
    }

    // same as TryParseAbsoluteDateTime, but without the year part
    public static (DateTimeOffset, string)? TryParseDayMonthTime(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a date + time";
            return null;
        }

        if (TryParseDateToken(args.Arguments[0], out errorString) is not int dateInt)
            return null;

        if (args.Arguments.Length < 2)
        {
            errorString = $"\"{argsString}\" is missing a month, time and UTC offset after the day";
            return null;
        }

        if (TryParseMonthToken(args.Arguments[1], out errorString) is not int monthInt)
            return null;

        var argsAfterMonth = string.Join("", argsString.Skip(args.Indices[1]));
        if (argsAfterMonth.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a time and UTC offset after the month";
            return null;
        }

        if (TryParseTimeToken(argsAfterMonth, out errorString) is not var (hours, minutes, argsAfterTime))
            return null;

        if (argsAfterTime.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a UTC offset after the time (e.g. \"UTC+0\", \"UTC-8\", \"UTC+11\", \"UTC-05:30\")";
            return null;
        }

        if (TryParseOffset(argsAfterTime, out errorString) is not var (offset, argsAfterOffset))
            return null;

        var dto = new DateTimeOffset(DateTimeHelper.UtcNow.Year, monthInt, dateInt, hours, minutes, 0, offset);

        if (dto < DateTimeHelper.UtcNow)
            dto = dto.AddYears(1);

        errorString = null;
        return (dto, argsAfterOffset);
    }

    public static (DateTimeOffset, string)? TryParseAbsoluteDateTime(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (!args.Arguments.Any())
        {
            errorString = $"empty or all-whitespace string \"{argsString}\" can't be a date + time";
            return null;
        }

        if (TryParseDateToken(args.Arguments[0], out errorString) is not int dateInt)
            return null;

        if (args.Arguments.Length < 2)
        {
            errorString = $"\"{argsString}\" is missing a month, year, time and UTC offset after the day";
            return null;
        }

        if (TryParseMonthToken(args.Arguments[1], out errorString) is not int monthInt)
            return null;

        if (args.Arguments.Length < 3)
        {
            errorString = $"\"{argsString}\" is missing a year, time and UTC offset after the month";
            return null;
        }

        var yearArg = args.Arguments[2];
        if (!int.TryParse(yearArg, out int yearInt))
        {
            errorString = $"\"{yearArg}\" is not a positive integer";
            return null;
        }
        if (yearInt < 0)
        {
            errorString = $"{yearInt} is negative; only positive years are supported";
            return null;
        }

        var argsAfterYear = string.Join("", argsString.Skip(args.Indices[2]));
        if (argsAfterYear.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a time and UTC offset after the year";
            return null;
        }

        if (TryParseTimeToken(argsAfterYear, out errorString) is not var (hours, minutes, argsAfterTime))
            return null;

        if (argsAfterTime.Trim() == "")
        {
            errorString = $"\"{argsString}\" is missing a UTC offset after the time (e.g. \"UTC+0\", \"UTC-8\", \"UTC+11\", \"UTC-05:30\")";
            return null;
        }

        if (TryParseOffset(argsAfterTime, out errorString) is not var (offset, argsAfterOffset))
            return null;

        var dto = new DateTimeOffset(yearInt, monthInt, dateInt, hours, minutes, 0, offset);

        errorString = null;
        return (dto, argsAfterOffset);
    }
}

public class TimeHelperResponse
{
    public string? RepeatType;
    public DateTimeOffset Time;

    public TimeHelperResponse(DateTimeOffset time, string? repeatType)
    {
        Time = time;
        RepeatType = repeatType;
    }
}