using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class ScheduledTask
{
    public ScheduledTask(DateTimeOffset createdAt, DateTimeOffset executeAt, ScheduledTaskAction action)
    {
        CreatedAt = createdAt;
        ExecuteAt = executeAt;
        Action = action;
    }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExecuteAt { get; set; }
    public ScheduledTaskAction Action { get; set; }
}

public class ScheduledTaskAction
{
    public ScheduledTaskAction(ScheduledTaskActionType type, Dictionary<string, string> fields)
    {
        Type = type;
        Fields = fields;
    }

    public ScheduledTaskActionType Type { get; set; }
    public Dictionary<string, string> Fields { get; set; }
}

public enum ScheduledTaskActionType
{
    RemoveRole,
    AddRole,
    Unban,
    Echo
}