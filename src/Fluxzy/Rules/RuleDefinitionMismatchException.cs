// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules
{
    public class RuleExecutionFailureException : Exception
    {
        public RuleExecutionFailureException(string message)
            : base(message)
        {
        }

        public RuleExecutionFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
