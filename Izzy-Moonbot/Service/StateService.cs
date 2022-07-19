namespace Izzy_Moonbot.Service
{
    using System.Collections.Generic;

    /*
     * The StateService is used for storing real-time volatile data
     * regarding Izzy's state across all services and commands.
     * This is extremely useful for debugging and preformance monitoring
     * as it gives a pretty clear idea on what Izzy is thinking in the
     * long-term (past a single message or command).
     */
    public class StateService
    {
        /* 
         * We don't actually know the content that services will store in
         * the StateService, so it's really up to the stuff using this
         * Service to handle getting the expected data type.
         */
        private Dictionary<string, object?> _states;

        public StateService()
        {
            _states = new Dictionary<string, object?>();
        }

        public bool doesStateExist(string stateName)
        {
            return _states.ContainsKey(stateName);
        }

        public object? getState(string stateName)
        {
            return _states[stateName];
        }

        public bool setState(string stateName, object? state)
        {
            return _states.TryAdd(stateName, state);
        }

        public Dictionary<string, object?> getAllStates()
        {
            return _states;
        }

        public List<string> getStateKeys()
        {
            List<string> keys = new();

            foreach (string key in _states.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }
    }
}
