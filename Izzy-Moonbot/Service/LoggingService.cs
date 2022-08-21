using System;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Helpers;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

public class LoggingService
{
    private readonly ILogger<Worker> _logger;

    public LoggingService(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public async Task Log(string message, SocketCommandContext context, bool file = false,
        LogLevel level = LogLevel.Information)
    {
        if (file) await AppendToFileAsync(message, context);

        var logMessage = PrepareMessageForLogging(message, context);
        //_logger.LogInformation(logMessage, level);
        _logger.Log(level, logMessage);
    }

    private static string PrepareMessageForLogging(string message, SocketCommandContext context, bool fileEntry = false,
        bool header = false)
    {
        var logMessage = "";
        if (!header)
        {
            //logMessage += $"[{DateTime.UtcNow:s}] ";
        }

        if (context != null)
        {
            if (context.IsPrivate)
            {
                if (!fileEntry)
                    logMessage +=
                        $"DM with @{context.User.Username}#{context.User.Discriminator} ({context.User.Id})";

                if (!fileEntry && !header) logMessage += ", ";

                if (!header) logMessage += message;
            }
            else
            {
                if (!fileEntry)
                    logMessage +=
                        $"server: {context.Guild.Name} ({context.Guild.Id}) #{context.Channel.Name} ({context.Channel.Id})";

                if (!fileEntry && !header) logMessage += " ";

                if (!header)
                {
                    logMessage += $"@{context.User.Username}#{context.User.Discriminator} ({context.User.Id}), ";
                    logMessage += message;
                }
            }
        }
        else
        {
            logMessage += message;
        }

        if (fileEntry || header) logMessage += Environment.NewLine;

        return logMessage;
    }

    private static async Task AppendToFileAsync(string message, SocketCommandContext context)
    {
        var filepath = FileHelper.SetUpFilepath(FilePathType.Channel, "<date>", "log", context);
        if (!File.Exists(filepath))
            await File.WriteAllTextAsync(filepath, PrepareMessageForLogging(message, context, false, true));

        var logMessage = PrepareMessageForLogging(message, context, true);
        await File.AppendAllTextAsync(filepath, logMessage);
    }
}