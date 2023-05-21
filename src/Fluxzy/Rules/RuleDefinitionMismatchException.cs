// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules
{
    public class RuleDefinitionMismatchException : Exception
    {
        public RuleDefinitionMismatchException(string message)
            : base(message)
        {
        }

        public RuleDefinitionMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
