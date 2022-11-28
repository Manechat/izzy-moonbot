using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class ScheduledJob
{
    public ScheduledJob(DateTimeOffset createdAt, DateTimeOffset executeAt, ScheduledJobAction action,
        ScheduledJobRepeatType repeatType = ScheduledJobRepeatType.None)
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = createdAt;
        LastExecutedAt = null;
        ExecuteAt = executeAt;
        Action = action;
        RepeatType = repeatType;
    }

    public string Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset ExecuteAt { get; set; }
    public ScheduledJobAction Action { get; set; }
    public ScheduledJobRepeatType RepeatType { get; set; }
}

public class ScheduledJobAction
{
    public ScheduledJobAction(ScheduledJobActionType type, Dictionary<string, string> fields)
    {
        Type = type;
        Fields = fields;
    }

    public ScheduledJobActionType Type { get; set; }
    public Dictionary<string, string> Fields { get; set; }
}

public enum ScheduledJobActionType
{
    RemoveRole,
    AddRole,
    Unban,
    Echo,
    BannerRotation
}

public enum ScheduledJobRepeatType
{
    None,
    Relative,
    Daily,
    Weekly,
    Yearly
}