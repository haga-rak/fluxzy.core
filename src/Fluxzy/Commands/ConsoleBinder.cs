// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.CommandLine;
using System.CommandLine.Binding;

namespace Fluxzy.Cli.Commands
{
    public class ConsoleBinder : BinderBase<IConsole>
    {
        protected override IConsole GetBoundValue(BindingContext bindingContext)
        {
            return bindingContext.Console;
        }
    }
}
