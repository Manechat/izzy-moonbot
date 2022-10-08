using System;

namespace Izzy_Moonbot.Helpers;

public static class TimeHelper
{
    /*private Regex timeFormatRegex = new Regex(
        "^(every |in |on |)(monday|tuesday|wednesday|thursday|friday|saturday|sunday|mon|tue|wed|thu|fri|sat|sun|)((a|\\d\\d|\\d|\\d\\dst|\\dst|\\d\\dnd|\\dnd|\\d\\drd|\\drd|\\d\\dth|\\dth) | at )(years|year|months|month|days|day|weeks|week|days|day|hours|hour|minutes|minute|seconds|second|january|february|march|april|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec|)( at |)(\\d\\d|\\d|\\dpm|\\d\\d:\\d\\dpm|\\d\\dpm|\\dam|\\d:\\d\\dam|\\d\\dam|\\d\\d:\\d\\dam|)$",
        RegexOptions.IgnoreCase);*/
}

public abstract class TimeHelperResponse
{
    public bool Repeats;
    public string? RepeatType;
    public DateTimeOffset Time;

    public TimeHelperResponse(DateTimeOffset time, bool repeats, string? repeatType)
    {
        Time = time;
        Repeats = repeats;
        RepeatType = repeatType;
    }
}