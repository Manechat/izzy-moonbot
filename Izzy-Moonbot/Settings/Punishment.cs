namespace Izzy_Moonbot.Settings
{
    using Service;
    using System;

    public class Punishment
    {
        public Punishment()
        {
            Action = ActionType.Silence;
            EndsAt = null;
        }

        public ActionType Action { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
    }
}
