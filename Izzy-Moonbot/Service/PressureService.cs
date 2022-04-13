namespace Izzy_Moonbot.Service
{
    using Izzy_Moonbot.Settings;
    using Izzy_Moonbot.Helpers;

    using Discord.WebSocket;

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class PressureService
    {
        private ServerSettings _settings;
        private Dictionary<ulong, User> _users;

        public PressureService(ServerSettings settings, Dictionary<ulong,User> users)
        {
            _settings = settings;
            _users = users;
        }

        /// <summary>
        /// Get the pressure of a given user by their Discord id.
        /// </summary>
        /// <param name="id">The Discord id of the user to get pressure for.</param>
        /// <returns>A <c>double</c> of the pressure. If the result is `-1` then the user hasn't been processed yet.</returns>
        public async Task<double> GetPressure(ulong id)
        {
            // If that user hasn't been processed yet, just return -1.
            if (!_users.ContainsKey(id)) return -1;

            var now = DateTime.UtcNow;
            var pressureLossPerSecond = _settings.SpamBasePressure / _settings.SpamPressureDecay;
            var pressure = _users[id].Pressure;
            var difference = now - _users[id].Timestamp;
            var totalPressureLoss = difference.TotalSeconds * pressureLossPerSecond;
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

        /// <summary>
        /// Increase the pressure of a user by their Discord id.
        /// </summary>
        /// <param name="id">The Discord id of the user to increase pressure for.</param>
        /// <param name="pressure">The pressure to add onto the current pressure.</param>
        /// <returns>A `double` of the new pressure.</returns>
        public async Task<double> IncreasePressure(ulong id, double pressure)
        {
            var now = DateTime.UtcNow;
            var currentPressure = await GetPressure(id);
            currentPressure += pressure;
            _users[id].Pressure = currentPressure;
            _users[id].Timestamp = now;

            await FileHelper.SaveUsersAsync(_users);

            return currentPressure;
        }

        private async Task<double> ProcessPressure(ulong id, SocketUserMessage message)
        {
            // This doesn't do much currently, but it will once we add image checks and the like
            return await IncreasePressure(id, _settings.SpamBasePressure);            
        }

        public async Task ProcessMessage(SocketUserMessage message)
        {
            var guildUser = message.Author as SocketGuildUser;
            var id = guildUser.Id;
            if (!_users.ContainsKey(id))
            {
                _users.Add(id, new User());
            }

            _users[id].Username = $"{guildUser.Username}#{guildUser.Discriminator}";
            if (!_users[id].Aliases.Contains(guildUser.Username))
            {
                _users[id].Aliases.Add(guildUser.Username);
            }
            if (guildUser.Nickname != null)
            {

                if (!_users[id].Aliases.Contains(guildUser.Nickname))
                {
                    _users[id].Aliases.Add(guildUser.Nickname);
                }
            }
            await ProcessPressure(id, message);
        }
    }
}