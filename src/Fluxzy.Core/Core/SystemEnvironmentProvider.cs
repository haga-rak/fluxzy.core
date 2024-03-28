using System;

namespace Fluxzy.Core
{
    internal class SystemEnvironmentProvider : EnvironmentProvider
    {
        public override string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}