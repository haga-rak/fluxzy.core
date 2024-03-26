using System;

namespace Fluxzy.Cli.Dockering
{
    internal class SystemEnvironmentProvider : EnvironmentProvider
    {
        public override string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}