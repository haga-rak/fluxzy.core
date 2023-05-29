// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluxzy.Rules
{
    /// <summary>
    ///     Retrieving variables and updating variables.
    ///     This object is a singleton according to the proxy instance .
    /// </summary>
    public class VariableContext
    {
        private static readonly Regex RegexVariable = new(@"\$\{(?<variableName>[a-zA-Z0-9_\.]+)\}",
            RegexOptions.Compiled);

        private readonly Dictionary<string, string> _systemVariables;
        private readonly Dictionary<string, string> _userSetVariables = new();

        public VariableContext()
        {
            _systemVariables = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                                          .ToDictionary(k =>
                                              "env." + k.Key!, v => v.Value?.ToString() ?? string.Empty)!;
        }

        public bool TryGet(string key, out string? value)
        {
            lock (_userSetVariables) {
                return _userSetVariables.TryGetValue(key, out value);
            }
        }

        public void Set(string key, string value)
        {
            // Update existing variables 

            lock (_userSetVariables) {
                _userSetVariables[key] = value;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="buildingParam"></param>
        /// <param name="contextualVariables"></param>
        /// <returns></returns>
        public string EvaluateVariable(
            string str,
            VariableBuildingContext? buildingParam)
        {
            // TODO : implement without regex 
            // TODO : add an escape character for the variable syntax

            return RegexVariable.Replace(str, match => {
                var variableName = match.Groups["variableName"].Value;

                if (buildingParam != null
                    && buildingParam.LazyVariableEvaluations.TryGetValue(variableName, out var func))
                    return func();

                if (TryGet(variableName, out var value))
                    return value!;

                if (_systemVariables.TryGetValue(variableName, out value))
                    return value!;

                return string.Empty;
            });
        }
    }
}
