using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
        _batchLogger = new BatchLogger(_config);
    }

    public BotLogBuilder CreateBotLog(SocketGuild guild)
    {
        return new BotLogBuilder(_config, guild, _batchLogger);
    }

    public ModLogBuilder CreateModLog(SocketGuild guild)
    {
        return new ModLogBuilder(_config, guild, _batchLogger);
    }
}

public class Log
{
    public SocketTextChannel Channel;
    public string? Content;
    public Embed? Embed;
    public string? FileLogContent;
    public FileAttachment? Attachment;

    public Log(SocketTextChannel channel) { Channel = channel; }
}

public class BotLogBuilder
{
    private readonly SocketGuild _guild;
    private readonly Config _config;
    private readonly BatchLogger _batchLogger;

    private readonly Log _log;

    public BotLogBuilder(Config config, SocketGuild guild, BatchLogger batchLogger)
    {
        _config = config;
        _guild = guild;
        _batchLogger = batchLogger;

        _log = new Log(_guild.GetTextChannel(_config.LogChannel));
    }

    public BotLogBuilder SetContent(string content)
    {
        _log.Content = content;
        return this;
    }

    public BotLogBuilder SetEmbed(Embed embed)
    {
        _log.Embed = embed;
        return this;
    }

    public BotLogBuilder SetFileLogContent(string content)
    {
        _log.FileLogContent = content;
        return this;
    }
    
    public BotLogBuilder SetFileAttachment(FileAttachment attachment)
    {
        _log.Attachment = attachment;
        return this;
    }

    public async Task Send()
    {
        if (_log.Content == null && _log.Embed == null) throw new InvalidOperationException("A bot log cannot have no content");

        if (_config.BatchSendLogs)
            _batchLogger.AddBotLog(_log);
        else
        {
            if (_log.Attachment != null)
            {
                await _log.Channel.SendFileAsync(_log.Attachment.Value, _log.Content, embed: _log.Embed);
            }
            else
            {
                await _log.Channel.SendMessageAsync(_log.Content, embed: _log.Embed);
            }
        }
    }
}

public class ModLogBuilder
{
    private readonly SocketGuild _guild;
    private readonly Config _config;
    private readonly BatchLogger _batchLogger;

    private readonly Log _log;

    public ModLogBuilder(Config config, SocketGuild guild, BatchLogger batchLogger)
    {
        _config = config;
        _guild = guild;
        _batchLogger = batchLogger;

        _log = new Log(_guild.GetTextChannel(_config.ModChannel));
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

    public ModLogBuilder SetFileAttachment(FileAttachment attachment)
    {
        _log.Attachment = attachment;
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
                await File.WriteAllTextAsync(filepath, $"----------= {DateTimeOffset.UtcNow:F} =----------{Environment.NewLine}");

            await File.AppendAllTextAsync(filepath, $"{modLogFileContent}{(_log.Attachment != null ? " [ATTACHMENT]" : "")}");
        }

        if (_config.BatchSendLogs)
            _batchLogger.AddModLog(_log);
        else
        {
            if (_log.Attachment != null)
            {
                await _log.Channel.SendFileAsync(_log.Attachment.Value, _log.Content, embed: _log.Embed);
            }
            else
            {
                await _log.Channel.SendMessageAsync(_log.Content, embed: _log.Embed);
            }
        }
    }
}

public class BatchLogger
{
    private readonly List<Log> _botLogs = new();
    private readonly List<Log> _modLogs = new();
    private readonly Config _config;

    public BatchLogger(Config config)
    {
        _config = config;

        RefreshBatchInterval();
    }

    public void AddBotLog(Log log)
    {
        _botLogs.Add(log);
    }
    
    public void AddModLog(Log log)
    {
        _modLogs.Add(log);
    }

    private void RefreshBatchInterval()
    {
        Task.Factory.StartNew(async () =>
        {
            await Task.Delay(Convert.ToInt32(_config.BatchLogsSendRate * 1000));

            SocketTextChannel? botLogChannel = null;
            var botLogContent = new List<string>();
            var botLogEmbeds = new List<Embed>();
            var botLogAttachments = new List<FileAttachment>();
            
            SocketTextChannel? modLogChannel = null;
            var modLogContent = new List<string>();
            var modLogEmbeds = new List<Embed>();
            var modLogAttachments = new List<FileAttachment>();

            foreach (var botLog in _botLogs)
            {
                botLogChannel = botLog.Channel;
                if (botLog.Embed is not null) botLogEmbeds.Add(botLog.Embed);
                if (botLog.Content is not null) botLogContent.Add(botLog.Content);
                if (botLog.Attachment is not null) botLogAttachments.Add(botLog.Attachment.Value);
            }
            
            foreach (var modLog in _modLogs)
            {
                modLogChannel = modLog.Channel;
                if (modLog.Embed is not null) modLogEmbeds.Add(modLog.Embed);
                if (modLog.Content is not null) modLogContent.Add(modLog.Content);
                if (modLog.Attachment is not null) modLogAttachments.Add(modLog.Attachment.Value);
            }
            
            if (botLogChannel != null)
            {
                if (botLogAttachments.Any())
                {
                    await botLogChannel.SendFilesAsync(botLogAttachments,
                        $"{string.Join(Environment.NewLine, botLogContent)}{Environment.NewLine}{Environment.NewLine}**Note:** Files may not be listed in the correct order.", embeds: botLogEmbeds.ToArray());
                }
                else
                {
                    await botLogChannel.SendMessageAsync(string.Join($"{Environment.NewLine}", botLogContent),
                        embeds: botLogEmbeds.ToArray());
                }
            }

            if (modLogChannel != null)
            {
                if (modLogAttachments.Any())
                {
                    await modLogChannel.SendFilesAsync(modLogAttachments,
                        $"{string.Join(Environment.NewLine, modLogContent)}{Environment.NewLine}{Environment.NewLine}**Note:** Files may not be listed in the correct order.", embeds: modLogEmbeds.ToArray());
                }
                else
                {
                    await modLogChannel.SendMessageAsync(string.Join($"{Environment.NewLine}", modLogContent),
                        embeds: modLogEmbeds.ToArray());
                }
            }

            _botLogs.Clear();
            _modLogs.Clear();

            RefreshBatchInterval();
        });
    }
}