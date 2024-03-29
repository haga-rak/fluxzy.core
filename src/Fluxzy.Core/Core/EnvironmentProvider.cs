// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public abstract class EnvironmentProvider
    {
        public virtual string ExpandEnvironmentVariables(string original)
        {
            // Find with regex matching %variablename% and replace everything with GetEnvironmentVariable
            // Warning, this implementation is meant for testing purposes only and may be invalid in some cases

            var regex = new System.Text.RegularExpressions.Regex("%([^%]+)%");

            return regex.Replace(original, match => {
                var variableName = match.Groups[1].Value;
                return GetEnvironmentVariable(variableName) ?? match.Value;
            });
        }

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
