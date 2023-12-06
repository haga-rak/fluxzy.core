// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    /// <summary>
    ///  Information about the environment where the archive was created.
    /// </summary>
    public class EnvironmentInformation
    {
        public EnvironmentInformation(string operationSystem, string runtimeIdentifier, string? machineName)
        {
            OperationSystem = operationSystem;
            RuntimeIdentifier = runtimeIdentifier;
            MachineName = machineName;
        }

        public string OperationSystem { get; }

        public string RuntimeIdentifier { get;  }

        public string ? MachineName { get; }
    }
}
