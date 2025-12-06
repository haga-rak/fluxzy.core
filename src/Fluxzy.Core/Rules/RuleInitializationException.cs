// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules
{
    /// <summary>
    /// Thrown when rule initialization fails during hot reload
    /// </summary>
    public class RuleInitializationException : Exception
    {
        public RuleInitializationException(string message) : base(message)
        {
        }

        public RuleInitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
