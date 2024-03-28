using System.Collections.Generic;

namespace Fluxzy.Core
{
    internal class DictionaryEnvironmentProvider : EnvironmentProvider
    {
        private readonly Dictionary<string, string> _environmentVariables;

        public DictionaryEnvironmentProvider(Dictionary<string, string> environmentVariables)
        {
            _environmentVariables = environmentVariables;
        }

        public override string? GetEnvironmentVariable(string variable)
        {
            return _environmentVariables.TryGetValue(variable, out var value) ? value : null;
        }
    }
}