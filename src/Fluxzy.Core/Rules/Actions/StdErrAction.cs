// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Write text to standard error. Captured variable are interpreted."
        , NonDesktopAction = true)]
    public class StdErrAction : MultipleScopeAction
    {
        public StdErrAction(string? text)
        {
            Text = text;
        }

        [ActionDistinctive]
        public string? Text { get; set; }

        public override string DefaultDescription => "Write to stderr";

        public override ValueTask MultiScopeAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Text != null)
                Console.Error.Write(Text.EvaluateVariable(context));

            return default;
        }
    }
}
