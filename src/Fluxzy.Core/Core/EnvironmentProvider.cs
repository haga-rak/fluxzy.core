// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    internal abstract class EnvironmentProvider
    {
        public virtual bool EnvironmentVariableActive(string name)
        {
            var variableValue = GetEnvironmentVariable(name);

            if (variableValue == null) {
                return false;
            }

            return variableValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || variableValue.Equals("1");
        }

        public abstract string? GetEnvironmentVariable(string variable);

        public bool TryGetEnvironmentVariable(string variable, out string value)
        {
            value = GetEnvironmentVariable(variable)!;

            return value != null!;
        }

        public bool TryGetInt32EnvironmentVariable(string variable, out int value)
        {
            var rawValue = GetEnvironmentVariable(variable);

            if (rawValue != null && int.TryParse(rawValue, out value)) {
                return true;
            }

            value = 0;

            return false;
        }

        public virtual int? GetInt32EnvironmentVariable(string variable)
        {
            var rawValue = GetEnvironmentVariable(variable);

            if (rawValue != null && int.TryParse(rawValue, out var value)) {
                return value;
            }

            return null;
        }
    }
}
