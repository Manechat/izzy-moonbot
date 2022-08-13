using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

// name credit to Scoots <3

public class ModLoggingService
{
    private readonly ServerSettings _settings;
    private BatchLogger _batchLogger;
    
    public ModLoggingService(ServerSettings settings)
    {
        _settings = settings;
        _batchLogger = new BatchLogger(_settings);
    }

    public ModLogConstructor CreateModLog(SocketGuild guild)
    {
        return new ModLogConstructor(_settings, guild, _batchLogger);
    }
    
    public ActionLogConstructor CreateActionLog(SocketGuild guild)
    {
        return new ActionLogConstructor(_settings, guild, _batchLogger);
    }
}

public class ModLog
{
    public SocketTextChannel Channel;
    public string Content;
    public Embed Embed;
}

public class ActionLog
{
    public SocketTextChannel Channel;
    public Embed Embed;
}

public class ModLogConstructor
{
    private readonly ServerSettings _settings;
    private readonly SocketGuild _guild;
    private BatchLogger _batchLogger;
    
    private ModLog _log = new ModLog();
    
    public ModLogConstructor(ServerSettings settings, SocketGuild guild, BatchLogger batchLogger)
    {
        _settings = settings;
        _guild = guild;
        _batchLogger = batchLogger;

        _log.Channel = _guild.GetTextChannel(_settings.ModChannel);
    }

    public ModLogConstructor SetContent(string content)
    {
        _log.Content = content;
        return this;
    }

    public ModLogConstructor SetEmbed(Embed embed)
    {
        _log.Embed = embed;
        return this;
    }

    public async Task Send()
    {
        if (_log.Content == null) throw new InvalidOperationException("A moderation log cannot have no content");

        if (_settings.BatchSendLogs)
        {
            _batchLogger.AddModLog(_log);
        }
        else
        {
            await _log.Channel.SendMessageAsync(_log.Content, embed: _log.Embed);
        }
    }
}


public class ActionLogConstructor
{
    private readonly ServerSettings _settings;
    private readonly SocketGuild _guild;
    private readonly BatchLogger _batchLogger;
    
    private ActionLog _log = new ActionLog();

    private LogType _actionType = LogType.Notice;
    private List<SocketGuildUser>? _targets;
    private DateTimeOffset _time = DateTimeOffset.Now;
    private DateTimeOffset? _until;
    private string _reason = "No reason provided.";
    private List<ulong>? _roles;
    private List<string>? _changeLog;
    
    public ActionLogConstructor(ServerSettings settings, SocketGuild guild, BatchLogger batchLogger)
    {
        _settings = settings;
        _guild = guild;
        _batchLogger = batchLogger;

        _log.Channel = _guild.GetTextChannel(_settings.LogChannel);
    }

    public ActionLogConstructor SetActionType(LogType type)
    {
        _actionType = type;
        return this;
    }

    public ActionLogConstructor AddTarget(SocketGuildUser target)
    {
        if (_targets == null) _targets = new List<SocketGuildUser>();
        _targets.Add(target);
        return this;
    }

    public ActionLogConstructor SetTime(DateTimeOffset time)
    {
        _time = time;
        return this;
    }

    public ActionLogConstructor SetUntilTime(DateTimeOffset? time)
    {
        _until = time;
        return this;
    }

    public ActionLogConstructor SetReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public ActionLogConstructor AddRole(ulong role)
    {
        if (_roles == null) _roles = new List<ulong>();
        _roles.Add(role);
        return this;
    }
    
    public ActionLogConstructor AddRoles(List<ulong> roles)
    {
        if (_roles == null) _roles = new List<ulong>();
        _roles.AddRange(roles);
        return this;
    }

    public async Task Send()
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();

        if (_settings.SafeMode)
            embedBuilder.WithDescription(
                "ℹ **This was an automated action Izzy Moonbot would have taken outside of `SafeMode`.**");
        else embedBuilder.WithDescription("ℹ **This was an automated action Izzy Moonbot took.**");

        if (_targets != null)
        {
            if (_targets.Count == 1)
            {
                embedBuilder.AddField("User", $"<@{_targets[0].Id}> (`{_targets[0].Id}`)", true);
            }
            else
            {
                var users = _targets.Select(target => $"<@{target.Id}> (`{target.Id}`)");
                embedBuilder.AddField("Users", string.Join(", ", users));
            }
        }
        embedBuilder.AddField("Action", GetLogTypeName(_actionType), true);
        embedBuilder.AddField("Occured At", $"<t:{_time.ToUnixTimeSeconds()}:F>", true);
        if (_until != null) embedBuilder.AddField("Ends At", $"<t:{_until.Value.ToUnixTimeSeconds()}:F>", true);
        if (_roles != null) embedBuilder.AddField("Roles", string.Join(", ", _roles.Select(role => $"<@&{role}>")));
        if (_changeLog != null)
        {
            embedBuilder.AddField("From", _changeLog[0]);
            embedBuilder.AddField("To", _changeLog[1]);
        }
        embedBuilder.AddField("Reason", _reason);

        embedBuilder.WithColor(GetLogColor(_actionType));

        _log.Embed = embedBuilder.Build();

        Console.WriteLine(_settings.BatchSendLogs);
        
        if (_settings.BatchSendLogs)
        {
            _batchLogger.AddActionLog(_log);
        }
        else
        {
            await _log.Channel.SendMessageAsync(embed: _log.Embed);
        }
    }
    
    private string GetLogTypeName(LogType action)
    {
        string output = "";
        switch (action)
        {
            case LogType.Notice:
                output = "Notice";
                break;
            case LogType.AddRoles:
                output = "Roles added";
                break;
            case LogType.RemoveRoles:
                output = "Roles removed";
                break;
            case LogType.Silence:
                output = "Silence";
                break;
            case LogType.Banish:
                output = "Banish";
                break;
            case LogType.Ban:
                output = "Ban";
                break;
            case LogType.Unban:
                output = "Unban";
                break;
            case LogType.VerificationLevel:
                output = "Change verification level";
                break;
            default:
                output = "what";
                break;
        }

        return output;
    }

    private Color GetLogColor(LogType action)
    {
        int output = 0x000000;
        switch (action)
        {
            case LogType.Notice:
            case LogType.AddRoles: 
            case LogType.RemoveRoles:
                output = 0x002920;
                break;
            case LogType.Silence:
                output = 0xffbb00;
                break;
            case LogType.Banish:
                output = 0xff8800;
                break;
            case LogType.VerificationLevel:
            case LogType.Ban:
                output = 0xaa0000;
                break;
            case LogType.Unban:
                output = 0x00ff00;
                break;
            default:
                output = 0x000000;
                break;
        }

        return new Color((uint)output);
    }
}

public class BatchLogger
{
    private readonly ServerSettings _settings;
    
    private readonly List<ModLog> _modLogs = new List<ModLog>();
    private readonly List<ActionLog> _actionLogs = new List<ActionLog>();

    public BatchLogger(ServerSettings settings)
    {
        _settings = settings;
        
        RefreshBatchInterval();
    }

    public void AddModLog(ModLog log)
    {
        _modLogs.Add(log);
    }

    public void AddActionLog(ActionLog log)
    {
        _actionLogs.Add(log);
    }

    private void RefreshBatchInterval()
    {
        Task.Factory.StartNew(async () =>
        {
            await Task.Delay(Convert.ToInt32(_settings.BatchLogsSendRate * 1000));
            // Do stuff*tm* to construct and create the batched stuff:tm:
            SocketTextChannel modLogChannel = null;
            SocketTextChannel actionLogChannel = null;

            var modLogContent = new List<string>();
            var modLogEmbeds = new List<Embed>();
            var actionLogEmbeds = new List<Embed>();

            foreach (var modLog in _modLogs)
            {
                modLogChannel = modLog.Channel;
                modLogEmbeds.Add(modLog.Embed);
                modLogContent.Add(modLog.Content);
            }

            foreach (var actionLog in _actionLogs)
            {
                actionLogChannel = actionLog.Channel;
                actionLogEmbeds.Add(actionLog.Embed);
            }

            if (modLogChannel != null)
            {
                await modLogChannel.SendMessageAsync(string.Join($"{Environment.NewLine}", modLogContent), embeds: modLogEmbeds.ToArray());
            }

            if (actionLogChannel != null)
            {
                await actionLogChannel.SendMessageAsync(embeds: actionLogEmbeds.ToArray());
            }

            _actionLogs.Clear();
            _modLogs.Clear();

            RefreshBatchInterval();
        });
    }
}

public enum LogType
{
    Notice,
    AddRoles,
    RemoveRoles,
    Silence,
    Banish,
    Ban,
    Unban,
    VerificationLevel,
}