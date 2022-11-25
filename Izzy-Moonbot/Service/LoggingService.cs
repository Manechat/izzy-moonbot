using System;
using System.Diagnostics;
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

    public async Task Log(string message, SocketCommandContext? context = null, LogLevel level = LogLevel.Information)
    {
        var logMessage = PrepareMessageForLogging(message, context);
        _logger.Log(level, logMessage);
    }

    public static string PrepareMessageForLogging(string message, SocketCommandContext? context, bool header = false)
    {
        var logMessage = "";
        if (header)
        {
            logMessage += $"[{DateTime.UtcNow:O}] ";
        }

        if (context != null)
        {
            if (context.IsPrivate)
            {
                logMessage += $"DM with @{context.User.Username}#{context.User.Discriminator} ({context.User.Id})";

                logMessage += ", ";

                logMessage += message;
            }
            else
            {
                logMessage += $"server: {context.Guild.Name} ({context.Guild.Id}) #{context.Channel.Name} ({context.Channel.Id})";

                logMessage += " ";

                logMessage += $"@{context.User.Username}#{context.User.Discriminator} ({context.User.Id}), ";
                logMessage += message;
            }
        }
        else
        {
            logMessage += message;
        }

        if (header) logMessage += Environment.NewLine;

        return logMessage;
    }
}