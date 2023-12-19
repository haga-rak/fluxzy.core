// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///   Ignore the default port used by the current authority and use the provided port instead
    /// </summary>
    [ActionMetadata("Ignores the default port used by the current authority and use the provided port instead.")]
    public class ForceRemotePortAction : Action
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public ForceRemotePortAction(int port)
        {
            Port = port;
        }

        [ActionDistinctive(Description = "The port to use for the remote connection")]
        public int Port { get; set;  }

        public override FilterScope ActionScope { get; } = FilterScope.OnAuthorityReceived;

        public override string DefaultDescription { get; } = "Force remote port";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Port <= 0 || Port > 65535) 
                throw new RuleExecutionFailureException("Port must be between 1 and 65535");

            context.RemoteHostPort = Port;

            return default;
        }
    }
}
