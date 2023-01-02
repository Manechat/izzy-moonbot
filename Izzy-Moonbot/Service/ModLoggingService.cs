using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

// name credit to Scoots <3

public class ModLoggingService
{
    private readonly Config _config;
    private readonly BatchLogger _batchLogger;

    public ModLoggingService(Config config)
    {
        _config = config;
        _batchLogger = new BatchLogger();
    }

    public ModLogBuilder CreateModLog(SocketGuild guild)
    {
        return CreateModLog(new SocketGuildAdapter(guild));
    }
    public ModLogBuilder CreateModLog(IIzzyGuild guild)
    {
        return new ModLogBuilder(_config, guild, _batchLogger);
    }
}

public class ModLog
{
    public IIzzySocketTextChannel Channel;
    public string? Content;
    public Embed? Embed;
    public string? FileLogContent;

    public ModLog(IIzzySocketTextChannel channel) { Channel = channel; }
}

public class ModLogBuilder
{
    private readonly IIzzyGuild _guild;
    private readonly Config _config;
    private readonly BatchLogger _batchLogger;

    private readonly ModLog _log;

    public ModLogBuilder(Config config, IIzzyGuild guild, BatchLogger batchLogger)
    {
        _config = config;
        _guild = guild;
        _batchLogger = batchLogger;

        var modChannel = _guild.GetTextChannel(_config.ModChannel);
        if (modChannel == null)
            throw new InvalidOperationException($"Failed to get mod channel from config value {_config.ModChannel}");
        _log = new ModLog(modChannel);
    }

    public ModLogBuilder SetContent(string content)
    {
        _log.Content = content;
        return this;
    }

    public ModLogBuilder SetEmbed(Embed embed)
    {
        _log.Embed = embed;
        return this;
    }

    public ModLogBuilder SetFileLogContent(string content)
    {
        _log.FileLogContent = content;
        return this;
    }

    public async Task Send()
    {
        if (_log.Content == null && _log.Embed == null) throw new InvalidOperationException("A moderation log cannot have no content");
        
        // Log to file
        if (_log.FileLogContent is string fileLogContent)
        {
            var modLogFileContent = LoggingService.PrepareMessageForLogging(fileLogContent, null, true);
            var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "moderation", "log");

            if (!File.Exists(filepath))
                await File.WriteAllTextAsync(filepath, $"----------= {DateTimeOffset.UtcNow:F} =----------\n");

            await File.AppendAllTextAsync(filepath, modLogFileContent);
        }

        // if we're in the middle of a raid serious enough that either Izzy herself or a human moderator
        // decided to enable auto-silencing, then also use batch logging to avoid being rate limited
        if (_config.AutoSilenceNewJoins)
            _batchLogger.AddModLog(_log);
        else
            await _log.Channel.SendMessageAsync(_log.Content ?? "", embeds: _log.Embed != null ? new []{ _log.Embed } : null);
    }
}

public class BatchLogger
{
    private readonly List<ModLog> _modLogs = new();

    private static readonly int _batchLogsSendRate = 10_000; // 10 seconds

    public BatchLogger()
    {
        RefreshBatchInterval();
    }

    public void AddModLog(ModLog log)
    {
        _modLogs.Add(log);
    }

    private void RefreshBatchInterval()
    {
        Task.Factory.StartNew(async () =>
        {
            await Task.Delay(_batchLogsSendRate);

            IIzzySocketTextChannel? modLogChannel = null;
            var modLogContent = new List<string>();
            var modLogEmbeds = new List<Embed>();

            foreach (var modLog in _modLogs)
            {
                modLogChannel = modLog.Channel;
                if (modLog.Embed is not null) modLogEmbeds.Add(modLog.Embed);
                if (modLog.Content is not null) modLogContent.Add(modLog.Content);
            }

            if (modLogChannel != null)
                await modLogChannel.SendMessageAsync(string.Join($"\n", modLogContent),
                    embeds: modLogEmbeds.ToArray());
            
            _modLogs.Clear();

            RefreshBatchInterval();
        });
    }
}