using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class ScheduledTask
{
    public ScheduledTask(DateTimeOffset createdAt, DateTimeOffset executeAt, ScheduledTaskAction action,
        bool repeatable = false)
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = createdAt;
        LastExecutedAt = null;
        ExecuteAt = executeAt;
        Action = action;
        Repeatable = repeatable;
    }

    public string Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset ExecuteAt { get; set; }
    public ScheduledTaskAction Action { get; set; }
    public bool Repeatable { get; set; }
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