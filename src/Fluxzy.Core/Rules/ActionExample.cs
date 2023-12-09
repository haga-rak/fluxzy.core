// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules
{
    /// <summary>
    ///    A class that represents an action example.
    /// </summary>
    public class ActionExample
    {
        public ActionExample(string description, Action action)
        {
            Description = description;
            Action = action;
        }

        public string Description { get; }

        public Action Action { get;  }
    }
}
