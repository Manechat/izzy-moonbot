using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

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

    public string Id { get; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset ExecuteAt { get; set; }
    public ScheduledJobAction Action { get; set; }
    public ScheduledJobRepeatType RepeatType { get; set; }
    
    public override string ToString()
    {
        return $"`{Id}`: {Action} <t:{ExecuteAt.ToUnixTimeSeconds()}:R>{(RepeatType != ScheduledJobRepeatType.None ? $", repeating {RepeatType.ToString()}{(LastExecutedAt != null ? $", last executed <t:{LastExecutedAt.Value.ToUnixTimeSeconds()}:R>": "")}" : "")}. Created <t:{CreatedAt.ToUnixTimeSeconds()}:F>";
    }
}

// Class only exists to be extended so we can have a single ScheduledJob class.
public class ScheduledJobAction
{ }

/* Scheduled Job Action types */
public class ScheduledRoleJob : ScheduledJobAction
{
    public ulong Role { get; protected set; }
    public ulong User { get; protected set; }
    public string? Reason { get; protected set; }
}

public class ScheduledRoleRemovalJob : ScheduledRoleJob
{
    public ScheduledRoleRemovalJob(ulong role, ulong user, string? reason = null)
    {
        Role = role;
        User = user;
        Reason = reason;
    }

    public ScheduledRoleRemovalJob(IRole role, IGuildUser user, string? reason = null)
    {
        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }

    public override string ToString()
    {
        return $"Remove <@&{Role}> (`{Role}`) from <@{User}> (`{User}`)";
    }
}

public class ScheduledRoleAdditionJob : ScheduledRoleJob
{
    public ScheduledRoleAdditionJob(ulong role, ulong user, string? reason = null)
    {
        Role = role;
        User = user;
        Reason = reason;
    }
    
    public ScheduledRoleAdditionJob(IRole role, IGuildUser user, string? reason = null)
    {
        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }
    
    public override string ToString()
    {
        return $"Add <@&{Role}> (`{Role}`) to <@{User}> (`{User}`)";
    }
}

public class ScheduledUnbanJob : ScheduledJobAction
{
    public ScheduledUnbanJob(ulong user)
    {
        User = user;
    }

    public ScheduledUnbanJob(IUser user)
    {
        User = user.Id;
    }
    
    public ulong User { get; }
    
    public override string ToString()
    {
        return $"Unban <@{User}> (`{User}`)";
    }
}

public class ScheduledEchoJob : ScheduledJobAction
{
    public ScheduledEchoJob(ulong channel, string content)
    {
        Channel = channel;
        Content = content;
    }

    public ScheduledEchoJob(IMessageChannel channel, string content)
    {
        Channel = channel.Id;
        Content = content;
    }

    public ScheduledEchoJob(IUser user, string content)
    {
        Channel = user.Id;
        Content = content;
    }
    
    public ulong Channel { get; }
    public string Content { get; }
    
    public override string ToString()
    {
        return $"Send \"{Content}\" to <#{Channel}> (`{Channel}`)";
    }
}

// Banner rotation doesn't need it's own data, but this class
// exists in order for the job to exist.
public class ScheduledBannerRotationJob : ScheduledJobAction
{
    public override string ToString()
    {
        return $"Run Banner Rotation";
    }
}

public enum ScheduledJobRepeatType
{
    None,
    Relative,
    Daily,
    Weekly,
    Yearly
}