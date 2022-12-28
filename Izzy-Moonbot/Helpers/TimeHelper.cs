using System;
using System.Linq;
using System.Text.RegularExpressions;
using Izzy_Moonbot.Helpers;

namespace Izzy_Moonbot.Helpers;

public static class TimeHelper
{
    public static (TimeHelperResponse, string)? TryParseDateTime(string argsString, out string? errorString)
    {
        var args = DiscordHelper.GetArguments(argsString);
        if (args.Arguments.Length < 2)
        {
            errorString = $"\"{argsString}\" can't be a datetime; all supported datetime formats have at least 2 tokens";
            return null;
        }

        TimeHelperResponse time;
        uint argsConsumed = 0;

        string twoArgs = "";
        string threeArgs = "";
        string fourArgs = "";
        string fiveArgs = "";
        try
        {
            twoArgs = string.Join(' ', args.Arguments.Take(2));
            time = Convert(twoArgs);
            argsConsumed = 2;
            if (args.Arguments.Length > 2 && args.Arguments.ElementAt(2) == "at")
                throw new FormatException("there's an 'at' next, so not just 2 args");
        }
        catch (FormatException fe2)
        {
            if (args.Arguments.Length <= 2)
            {
                errorString = $"Failed to extract a date/time from \"{argsString}\"\n" +
                    $"\"{twoArgs}\" failed with {fe2.Message}";
                return null;
            }

            try
            {
                threeArgs = string.Join(' ', args.Arguments.Take(3));
                time = Convert(threeArgs);
                argsConsumed = 3;
            }
            catch (FormatException fe3)
            {
                if (args.Arguments.Length == 3)
                {
                    errorString = $"Failed to extract a date/time from \"{argsString}\"\n" +
                        $"\"{twoArgs}\" failed with {fe2.Message}\n" +
                        $"\"{threeArgs}\" failed with {fe3.Message}";
                    return null;
                }

                try
                {
                    fourArgs = string.Join(' ', args.Arguments.Take(4));
                    time = Convert(fourArgs);
                    argsConsumed = 4;
                }
                catch (FormatException fe4)
                {
                    if (args.Arguments.Length == 4)
                    {
                        errorString = $"Failed to extract a date/time from \"{argsString}\"\n" +
                            $"\"{twoArgs}\" failed with {fe2.Message}\n" +
                            $"\"{threeArgs}\" failed with {fe3.Message}\n" +
                            $"\"{fourArgs}\" failed with {fe4.Message}";
                        return null;
                    }

                    try
                    {
                        fiveArgs = string.Join(' ', args.Arguments.Take(5));
                        time = Convert(fiveArgs);
                        argsConsumed = 5;
                    }
                    catch (FormatException fe5)
                    {
                        errorString = $"Failed to extract a date/time from \"{argsString}\"\n" +
                            $"\"{twoArgs}\" failed with {fe2.Message}\n" +
                            $"\"{threeArgs}\" failed with {fe3.Message}\n" +
                            $"\"{fourArgs}\" failed with {fe4.Message}\n" +
                            $"\"{fiveArgs}\" failed with {fe5.Message}";
                        return null;
                    }
                }
            }
        }

        errorString = null;
        var remainingArgsString = string.Join("", argsString.Skip(args.Indices[argsConsumed - 1]));
        return (time, remainingArgsString);
    }

    public static string GetTimeType(string input)
    {
        var timeFormatRegex = new Regex(
            "(?<query1>every |in |on |on the |at |)",
            RegexOptions.IgnoreCase);

        switch (input.Split(" ")[0].ToLower())
        {
            case "every":
                return "repeat";
            case "in":
                return "relative";
            case "at":
                return "absolute time";
            case "on":
            case "on the":
                return "absolute date";
            default:
                return "unknown";
        }
    }
    
    public static TimeHelperResponse Convert(string input)
    {
        var timeFormatRegex = new Regex(
            "^(?<query1>every |in |on |on the |at |)(?<weekday>monday|tuesday|wednesday|thursday|friday|saturday|sunday|mon|tue|wed|thu|fri|sat|sun|)(?<query2>(?<date>a |\\d\\d\\d\\d |\\d\\d\\d |\\d\\d |\\d |\\d\\dst |\\dst |\\d\\dnd |\\dnd |\\d\\drd |\\drd |\\d\\dth |\\dth |)| at )(of )?(?<month>years|year|months|month|days|day|weeks|week|days|day|hours|hour|minutes|minute|seconds|second|january|february|march|april|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec|)(?<year> \\d\\d| \\d\\d\\d\\d|)(?<query3> at |)(?<time>\\d\\d:\\d\\d|\\d:\\d\\d|\\d\\d|\\d|\\dpm|\\d:\\d\\dpm|\\d\\d:\\d\\dpm|\\d\\dpm|\\dam|\\d:\\d\\dam|\\d\\dam|\\d\\d:\\d\\dam|)$",
            RegexOptions.IgnoreCase);

        var relativeMonths = new[]
        {
            "years", "year", "months", "month", "days", "day", "weeks", "week", "days", "day", "hours", "hour", "minutes", "minute", "seconds", "second"
        };

        var absoluteMonths = new[]
        {
            "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december",
            "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"
        };

        if (timeFormatRegex.IsMatch(input))
        {
            var match = timeFormatRegex.Match(input);
            
            var query1 = match.Groups["query1"];
            var weekday = match.Groups["weekday"];
            var query2 = match.Groups["query2"];
            var date = match.Groups["date"];
            var month = match.Groups["month"];
            var year = match.Groups["year"];
            var query3 = match.Groups["query3"];
            var time = match.Groups["time"];

            if (query1.Success)
            {
                // We likely already know what to expect
                if (query1.Value.Trim().ToLower() == "in")
                {
                    // Relative time
                    if (date.Value.Trim().ToLower() == "" || month.Value.Trim().ToLower() == "")
                        throw new FormatException(
                            "BAD_FORMAT: The provided string doesn't match any possible date format.");
                    if (!relativeMonths.Contains(month.Value.Trim().ToLower()))
                        throw new FormatException(
                            "UNKNOWN_RELATIVE_MONTH: The provided relative month isn't a relative month.");
                    return ConvertRelative(date.Value.Trim().ToLower(), month.Value.Trim().ToLower());
                }

                if (query1.Value.Trim().ToLower() == "on" || query1.Value.Trim().ToLower() == "on the")
                {
                    // Absolute time, but can be exact date or relative to week
                    if (weekday.Value.Trim().ToLower() != "" &&
                        query3.Value.Trim().ToLower() == "at" &&
                        time.Value.Trim().ToLower() != "")
                    {
                        // Most likely a weekly 
                        return ConvertWeekRelative(weekday.Value.Trim().ToLower(), time.Value.Trim().ToLower());
                    }

                    if (
                        date.Value.Trim().ToLower() != "" &&
                        query3.Value.Trim().ToLower() == "at" &&
                        time.Value.Trim().ToLower() != ""
                    )
                    {
                        if (!absoluteMonths.Contains(month.Value.Trim().ToLower()))
                            throw new FormatException("UNKNOWN_ABSOLUTE_MONTH: The provided month isn't a month.");

                        return ConvertAbsolute(date.Value.Trim().ToLower(), month.Value.Trim().ToLower(),
                            year.Value.Trim().ToLower() != ""
                                ? year.Value.Trim().ToLower()
                                : DateTimeHelper.UtcNow.Year.ToString(), time.Value.Trim().ToLower());
                    }
                    
                    throw new FormatException("UNKNOWN_QUERY: The provided string doesn't match any possible date format.");
                }

                if (query1.Value.Trim().ToLower() == "at")
                {
                    // Accurate time, relative date
                    if (time.Value.Trim().ToLower() == "")
                        throw new FormatException(
                            "BAD_FORMAT: The provided string doesn't match any possible date format.");
                    return ConvertTimeRelative(time.Value.Trim().ToLower());
                }
                if (query1.Value.Trim().ToLower() == "every")
                {
                    if (
                        month.Value.Trim().ToLower() != "" &&
                        relativeMonths.Contains(month.Value.Trim().ToLower()) &&
                        query3.Value.Trim().ToLower() == "" && time.Value.Trim().ToLower() == ""
                        )
                        return ConvertRepeatingRelative(date.Value.Trim().ToLower(), month.Value.Trim().ToLower());

                    if (
                        weekday.Value.Trim().ToLower() != "" &&
                        query3.Value.Trim().ToLower() == "at" &&
                        time.Value.Trim().ToLower() != ""
                        )
                        return ConvertRepeatingWeekRelative(weekday.Value.Trim().ToLower(),
                            time.Value.Trim().ToLower());

                    if (
                        time.Value.Trim().ToLower() != "" &&
                        month.Value.Trim().ToLower() == "day" &&
                        query3.Value.Trim().ToLower() == "at" &&
                        weekday.Value.Trim().ToLower() == "" &&
                        date.Value.Trim().ToLower() == ""
                    )
                        return ConvertRepeatingTimeRelative(time.Value.Trim().ToLower());
                    
                    if (
                        date.Value.Trim().ToLower() != "" &&
                        absoluteMonths.Contains(month.Value.Trim().ToLower()) &&
                        query3.Value.Trim().ToLower() == "at" &&
                        time.Value.Trim().ToLower() != ""
                        )
                        return ConvertRepeatingAbsolute(date.Value.Trim().ToLower(), month.Value.Trim().ToLower(),
                            time.Value.Trim().ToLower());
                    
                    throw new FormatException("UNKNOWN_QUERY: The provided string doesn't match any possible date format.");
                }
                
                // We must judge from what we've been given to work out what to expect
                /*
                     * Relative:
                     *      query2 and date match
                     *      month is "years|year|months|month|days|day|weeks|week|days|day|hours|hour|minutes|minute|seconds|second"
                     *      rest unset
                     *
                     * Relative week:
                     *      weekday is set
                     *      query2 is "at"
                     *      time is set
                     *      rest unset
                     *
                     * Absolute:
                     *      query2 and date match
                     *      month is "january|february|march|april|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec"
                     *      query3 is "at"
                     *      time is set
                     *      rest unset
                     */

                if (
                    date.Value.Trim().ToLower() != "" && month.Value.Trim().ToLower() != "" &&
                    relativeMonths.Contains(month.Value.Trim().ToLower()) &&
                    query3.Value.Trim().ToLower() == "" && time.Value.Trim().ToLower() == ""
                )
                    return ConvertRelative(date.Value.Trim().ToLower(), month.Value.Trim().ToLower());

                if (weekday.Value.Trim().ToLower() != "" &&
                    query3.Value.Trim().ToLower() == "at" &&
                    time.Value.Trim().ToLower() != ""
                    )
                    return ConvertWeekRelative(weekday.Value.Trim().ToLower(), time.Value.Trim().ToLower());

                if (
                    time.Value.Trim().ToLower() != "" &&
                    weekday.Value.Trim().ToLower() == "" &&
                    date.Value.Trim().ToLower() == ""
                )
                    return ConvertTimeRelative(time.Value.Trim().ToLower());
                
                if (
                    date.Value.Trim().ToLower() != "" &&
                    absoluteMonths.Contains(month.Value.Trim().ToLower()) &&
                    query3.Value.Trim().ToLower() == "at" &&
                    time.Value.Trim().ToLower() != ""
                )
                    return ConvertAbsolute(date.Value.Trim().ToLower(), month.Value.Trim().ToLower(),
                        year.Value.Trim().ToLower() != ""
                            ? year.Value.Trim().ToLower()
                            : DateTimeHelper.UtcNow.Year.ToString(),
                        time.Value.Trim().ToLower());

                throw new FormatException("UNKNOWN_QUERY: The provided string doesn't match any possible date format.");
            }

            throw new FormatException("NO_REGEX_MATCH: The provided string doesn't match any possible date format.");
        }

        throw new FormatException("NO_REGEX_MATCH: The provided string doesn't match any possible date format.");
    }

    private static TimeHelperResponse ConvertRelative(string date, string month)
    {
        var dateInt = date == "a" ? 1 : 0;
        if (dateInt == 0)
        {
            if (!int.TryParse(date, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }

        var dateTime = DateTimeHelper.UtcNow;
        
        switch (month)
        {
            case "years":
            case "year":
                dateTime = dateTime.AddYears(dateInt);
                break;
            case "months":
            case "month":
                dateTime = dateTime.AddMonths(dateInt);
                break;
            case "weeks":
            case "week":
                dateTime = dateTime.AddDays(dateInt * 7);
                break;
            case "days":
            case "day":
                dateTime = dateTime.AddDays(dateInt);
                break;
            case "hours":
            case "hour":
                dateTime = dateTime.AddHours(dateInt);
                break;
            case "minutes":
            case "minute":
                dateTime = dateTime.AddMinutes(dateInt);
                break;
            case "seconds":
            case "second":
                dateTime = dateTime.AddSeconds(dateInt);
                break;
            default:
                throw new FormatException("UNKNOWN_RELATIVE_MONTH: Unable to convert a relative month string into a DateTimeOffset.");
        }

        return new TimeHelperResponse(dateTime, null);
    }

    private static TimeHelperResponse ConvertWeekRelative(string weekday, string time)
    {
        var weekdayInt = weekday switch
        {
            "sunday" => 0,
            "monday" => 1,
            "tuesday" => 2,
            "wednesday" => 3,
            "thursday" => 4,
            "friday" => 5,
            "saturday" => 6,
            _ => throw new FormatException("UNKNOWN_WEEKDAY: Weekday provided is unknown.")
        };

        var daysToAdd = 0;

        var dateTime = DateTimeHelper.UtcNow;
        var currentWeekDay = (int) dateTime.DayOfWeek;
        
        // monday -> friday = 4
        // friday -> monday = -4 (-4+7 = 3)
        if (weekdayInt - currentWeekDay >= 0)
            daysToAdd = weekdayInt - currentWeekDay;
        else
            daysToAdd = (weekdayInt - currentWeekDay) + 7;

        dateTime = dateTime.AddDays(daysToAdd);
        
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];

        var outputDateTime = new DateTimeOffset(dateTime.Year, dateTime.Month,
            dateTime.Day, hourInt, minuteInt, 0, TimeSpan.Zero);
        
        return new TimeHelperResponse(outputDateTime, null);
    }

    private static TimeHelperResponse ConvertTimeRelative(string time)
    {
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];

        var outputDateTime = new DateTimeOffset(DateTimeHelper.UtcNow.Year, DateTimeHelper.UtcNow.Month,
            DateTimeHelper.UtcNow.Day, hourInt, minuteInt, 0, TimeSpan.Zero);
        
        return new TimeHelperResponse(outputDateTime, null);
    }

    private static TimeHelperResponse ConvertAbsolute(string date, string month, string year, string time)
    {
        // Convert date to something easier to process.
        var dateInt = 0;
        if (date.EndsWith("st") ||
            date.EndsWith("nd") ||
            date.EndsWith("rd") ||
            date.EndsWith("th"))
        {
            var dateWithoutSuffix = date.Remove(date.Length - 2, 2);

            if (!int.TryParse(dateWithoutSuffix, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        else
        {
            if (!int.TryParse(date, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        
        // Convert month to something easier to process.
        var monthInt = month switch
        {
            "january" or "jan" => 1,
            "february" or "feb" => 2,
            "march" or "mar" => 3,
            "april" or "apr" => 4,
            "may" => 5,
            "june" or "jun" => 6,
            "july" or "jul" => 7,
            "august" or "aug" => 8,
            "september" or "sep" => 9,
            "october" or "oct" => 10,
            "november" or "nov" => 11,
            "december" or "dec" => 12,
            _ => throw new FormatException("UNKNOWN_MONTH: Month provided is unknown.")
        };

        var yearInt = 0;

        if (year.Length == 2)
        {
            var fullYear = $"20{year}";
            
            if (!int.TryParse(fullYear, out yearInt))
                throw new FormatException("YEAR_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        else
        {
            if (!int.TryParse(year, out yearInt))
                throw new FormatException("YEAR_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];
        
        if (yearInt == 0) throw new FormatException("YEAR_INVALID: Year is invalid.");
        if (dateInt == 0) throw new FormatException("DAY_INVALID: Day is invalid.");

        var dateTime = new DateTimeOffset(yearInt, monthInt, dateInt, hourInt, minuteInt, 0, TimeSpan.Zero);
        
        return new TimeHelperResponse(dateTime, null);
    }

    private static int[] ConvertTime(string time)
    {
        var timeRegex = new Regex("^(?<hour>\\d\\d|\\d)(:(?<minute>\\d\\d))?(?<period>am|pm)?$",
            RegexOptions.IgnoreCase);

        var hourInt = 0;
        var minuteInt = 0;

        if (timeRegex.IsMatch(time))
        {
            var match = timeRegex.Match(time);

            var hour = match.Groups["hour"];
            var minute = match.Groups["minute"];
            var period = match.Groups["period"];

            if(hour.Value.Trim().ToLower() == "")
                throw new FormatException("HOUR_MISSING: The time provided doesn't have an hour.");

            if (!int.TryParse(hour.Value.Trim().ToLower(), out hourInt))
                throw new FormatException("HOUR_NOT_INT: The hour should be an integer.");
            
            if (period.Value.Trim().ToLower() == "pm" && (hourInt >= 1 && hourInt <= 11))
            {
                hourInt += 12;
            }
            
            if (hourInt == 12 && period.Value.Trim().ToLower() == "am")
            {
                hourInt = 0;
            }

            if (minute.Value.Trim().ToLower() != "")
            {
                if (!int.TryParse(minute.Value.Trim().ToLower(), out minuteInt))
                    throw new FormatException("MINUTE_NOT_INT: The minute should be an integer.");
            }
        }
        else
            throw new FormatException("TIME_REGEX_FAIL: The time provided couldn't be processed by regex");

        return new[]
        {
            hourInt,
            minuteInt
        };
    }
    
    private static TimeHelperResponse ConvertRepeatingRelative(string date, string month)
    {
        var dateInt = date == "" ? 1 : 0;
        if (dateInt == 0)
        {
            if (!int.TryParse(date, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }

        var dateTime = DateTimeHelper.UtcNow;
        
        switch (month)
        {
            case "years":
            case "year":
                dateTime = dateTime.AddYears(dateInt);
                break;
            case "months":
            case "month":
                dateTime = dateTime.AddMonths(dateInt);
                break;
            case "weeks":
            case "week":
                dateTime = dateTime.AddDays(dateInt * 7);
                break;
            case "days":
            case "day":
                dateTime = dateTime.AddDays(dateInt);
                break;
            case "hours":
            case "hour":
                dateTime = dateTime.AddHours(dateInt);
                break;
            case "minutes":
            case "minute":
                dateTime = dateTime.AddMinutes(dateInt);
                break;
            case "seconds":
            case "second":
                dateTime = dateTime.AddSeconds(dateInt);
                break;
            default:
                throw new FormatException("UNKNOWN_RELATIVE_MONTH: Unable to convert a relative month string into a DateTimeOffset.");
        }

        return new TimeHelperResponse(dateTime, "relative");
    }

    private static TimeHelperResponse ConvertRepeatingWeekRelative(string weekday, string time)
    {
        var weekdayInt = weekday switch
        {
            "sunday" => 0,
            "monday" => 1,
            "tuesday" => 2,
            "wednesday" => 3,
            "thursday" => 4,
            "friday" => 5,
            "saturday" => 6,
            _ => throw new FormatException("UNKNOWN_WEEKDAY: Weekday provided is unknown.")
        };

        var daysToAdd = 0;

        var dateTime = DateTimeHelper.UtcNow;
        var currentWeekDay = (int) dateTime.DayOfWeek;
        
        // monday -> friday = 4
        // friday -> monday = -4 (-4+7 = 3)
        if (weekdayInt - currentWeekDay >= 0)
            daysToAdd = weekdayInt - currentWeekDay;
        else
            daysToAdd = (weekdayInt - currentWeekDay) + 7;

        dateTime = dateTime.AddDays(daysToAdd);
        
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];

        var outputDateTime = new DateTimeOffset(dateTime.Year, dateTime.Month,
            dateTime.Day, hourInt, minuteInt, 0, TimeSpan.Zero);

        return new TimeHelperResponse(outputDateTime, "weekly");
    }
    
    private static TimeHelperResponse ConvertRepeatingTimeRelative(string time)
    {
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];

        var outputDateTime = new DateTimeOffset(DateTimeHelper.UtcNow.Year, DateTimeHelper.UtcNow.Month,
            DateTimeHelper.UtcNow.Day, hourInt, minuteInt, 0, TimeSpan.Zero);
        
        return new TimeHelperResponse(outputDateTime, "daily");
    }

    private static TimeHelperResponse ConvertRepeatingAbsolute(string date, string month, string time)
    {
        // Convert date to something easier to process.
        var dateInt = 0;
        if (date.EndsWith("st") ||
            date.EndsWith("nd") ||
            date.EndsWith("rd") ||
            date.EndsWith("th"))
        {
            var dateWithoutSuffix = date.Remove(date.Length - 2, 2);

            if (!int.TryParse(dateWithoutSuffix, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        else
        {
            if (!int.TryParse(date, out dateInt))
                throw new FormatException("DATE_NOT_INT: Couldn't convert what should be a number to a number.");
        }
        
        // Convert month to something easier to process.
        var monthInt = month switch
        {
            "january" or "jan" => 1,
            "february" or "feb" => 2,
            "march" or "mar" => 3,
            "april" or "apr" => 4,
            "may" => 5,
            "june" or "jun" => 6,
            "july" or "jul" => 7,
            "august" or "aug" => 8,
            "september" or "sep" => 9,
            "october" or "oct" => 10,
            "november" or "nov" => 11,
            "december" or "dec" => 12,
            _ => throw new FormatException("UNKNOWN_MONTH: Month provided is unknown.")
        };
        
        var convertedTime = ConvertTime(time);
        var hourInt = convertedTime[0];
        var minuteInt = convertedTime[1];

        if (dateInt == 0) throw new FormatException("DAY_INVALID: Day is invalid.");

        var dateTime = new DateTimeOffset(DateTimeHelper.UtcNow.Year, monthInt, dateInt, hourInt, minuteInt, 0, TimeSpan.Zero);

        if (DateTimeHelper.UtcNow.ToUnixTimeMilliseconds() > dateTime.ToUnixTimeMilliseconds()) 
            dateTime = dateTime.AddYears(1);

        return new TimeHelperResponse(dateTime, "yearly");
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