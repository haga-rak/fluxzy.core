// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    [ActionMetadata("Reply with fluxzy welcome page")]
    public class MountWelcomePageAction : Action
    {
        public override FilterScope ActionScope => InternalScope;

        [JsonIgnore]
        [YamlIgnore]
        internal FilterScope InternalScope { get; set; } = FilterScope.DnsSolveDone;

        public override string DefaultDescription => "Reply with welcome page";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (context.PreMadeResponse == null)
            {
                var bodyContent = BodyContent.CreateFromString(FileStore.welcome);

                context.PreMadeResponse = new MockedResponseContent(200,
                    bodyContent)
                {
                    Headers = {
                        new ("Content-Type","text/html")
                    }
                };
            }

            return default;
        }
    }
}
