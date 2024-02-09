// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Writers;

namespace Fluxzy.Rules
{
    public class StartupContext
    {
        public StartupContext(FluxzySetting setting, VariableContext variableContext, RealtimeArchiveWriter archiveWriter)
        {
            Setting = setting;
            VariableContext = variableContext;
            ArchiveWriter = archiveWriter;
        }

        public FluxzySetting Setting { get;  }

        public VariableContext VariableContext { get; }

        public RealtimeArchiveWriter ArchiveWriter { get; }
    }
}
