using Discord;
using Discord.Commands;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Modules
{
    public class SpamModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly ServerSettings _settings;
        private readonly Dictionary<ulong, User> _users;

        public SpamModule(LoggingService logger, ServerSettings settings, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _settings = settings;
            _users = users;
        }
        [Command("getpressure")]
        [Summary("get user pressure")]
        public async Task GetPressureAsync([Summary("userid")][Remainder] string userName)
        {
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            var pressure = await GetCurrentPressure(userId);
            if (pressure < 0)
            {
                await ReplyAsync($"Could not find user <@{userId}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"<@{userId}> Current Pressure: {pressure}", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task<double> GetCurrentPressure(ulong id)
        {
            if (!_users.ContainsKey(id))
            {
                return -1;
            }

            var now = DateTime.UtcNow;
            var pressureLossPerSecond = _settings.SpamBasePressure / _settings.SpamPressureDecay;
            var pressure = _users[id].Pressure;
            var difference = now - _users[id].Timestamp;
            var totalSeconds = difference.TotalSeconds;
            var totalPressureLoss = totalSeconds * pressureLossPerSecond;
            pressure -= totalPressureLoss;
            if (pressure < 0)
            {
                pressure = 0;
            }

            _users[id].Pressure = pressure;
            _users[id].Timestamp = now;
            await FileHelper.SaveUsersAsync(_users);
            return pressure;
        }
    }
}
