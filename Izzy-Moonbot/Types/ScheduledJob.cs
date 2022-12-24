using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;

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
    
    public string ToDiscordString()
    {
        return $"`{Id}`: {Action.ToDiscordString()} <t:{ExecuteAt.ToUnixTimeSeconds()}:R>{(RepeatType != ScheduledJobRepeatType.None ? $", repeating {RepeatType.ToString()}{(LastExecutedAt != null ? $", last executed <t:{LastExecutedAt.Value.ToUnixTimeSeconds()}:R>": "")}" : "")}. Created <t:{CreatedAt.ToUnixTimeSeconds()}:F>";
    }

    public string ToFileString()
    {
        return $"{Id}: {Action.ToFileString()} at {ExecuteAt:F}{(RepeatType != ScheduledJobRepeatType.None ? $", repeating {RepeatType.ToString()}{(LastExecutedAt != null ? $", last executed at {LastExecutedAt.Value:F}": "")}" : "")}. Created at {CreatedAt:F}";
    }
}

// Class only exists to be extended so we can have a single ScheduledJob class.
public class ScheduledJobAction
{
    public ScheduledJobActionType Type { get; protected set; }

    public virtual string ToDiscordString()
    {
        return "Unknown Scheduled Job Action";
    }

    public virtual string ToFileString()
    {
        return ToDiscordString();
    }
}

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
        Type = ScheduledJobActionType.RemoveRole;
        
        Role = role;
        User = user;
        Reason = reason;
    }

    public ScheduledRoleRemovalJob(IRole role, IGuildUser user, string? reason = null)
    {
        Type = ScheduledJobActionType.RemoveRole;
        
        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }

    public override string ToDiscordString()
    {
        return $"Remove <@&{Role}> (`{Role}`) from <@{User}> (`{User}`)";
    }

    public override string ToFileString()
    {
        return $"Remove role {Role} from user {User}";
    }
}

public class ScheduledRoleAdditionJob : ScheduledRoleJob
{
    public ScheduledRoleAdditionJob(ulong role, ulong user, string? reason = null)
    {
        Type = ScheduledJobActionType.AddRole;
        
        Role = role;
        User = user;
        Reason = reason;
    }
    
    public ScheduledRoleAdditionJob(IIzzyRole role, IIzzyGuildUser user, string? reason = null)
    {
        Type = ScheduledJobActionType.AddRole;
        
        Role = role.Id;
        User = user.Id;
        Reason = reason;
    }
    
    public override string ToDiscordString()
    {
        return $"Add <@&{Role}> (`{Role}`) to <@{User}> (`{User}`)";
    }
    
    public override string ToFileString()
    {
        return $"Add role {Role} to user {User}";
    }
}

public class ScheduledUnbanJob : ScheduledJobAction
{
    public ScheduledUnbanJob(ulong user)
    {
        Type = ScheduledJobActionType.Unban;
        
        User = user;
    }

    public ScheduledUnbanJob(IIzzyUser user)
    {
        Type = ScheduledJobActionType.Unban;
        
        User = user.Id;
    }
    
    public ulong User { get; }
    
    public override string ToDiscordString()
    {
        return $"Unban <@{User}> (`{User}`)";
    }
    
    public override string ToFileString()
    {
        return $"Unban user {User}";
    }
}

public class ScheduledEchoJob : ScheduledJobAction
{
    public ScheduledEchoJob(ulong channel, string content)
    {
        Type = ScheduledJobActionType.Echo;
        
        Channel = channel;
        Content = content;
    }

    public ScheduledEchoJob(IIzzyMessageChannel channel, string content)
    {
        Type = ScheduledJobActionType.Echo;
        
        Channel = channel.Id;
        Content = content;
    }

    public ScheduledEchoJob(IIzzyUser user, string content)
    {
        Type = ScheduledJobActionType.Echo;
        
        Channel = user.Id;
        Content = content;
    }
    
    public ulong Channel { get; }
    public string Content { get; }
    
    public override string ToDiscordString()
    {
        return $"Send \"{Content}\" to <#{Channel}> (`{Channel}`)";
    }
    
    public override string ToFileString()
    {
        return $"Send \"{Content}\" to channel/user {Channel}";
    }
}

// Banner rotation doesn't need it's own data, but this class
// exists in order for the job to exist.
public class ScheduledBannerRotationJob : ScheduledJobAction
{
    public ScheduledBannerRotationJob()
    {
        Type = ScheduledJobActionType.BannerRotation;
    }
    
    public override string ToDiscordString()
    {
        return $"Run Banner Rotation";
    }
    
    public override string ToFileString()
    {
        return ToDiscordString();
    }
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