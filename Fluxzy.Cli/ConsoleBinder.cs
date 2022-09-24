// Copyright © 2022 Haga Rakotoharivelo

using System.CommandLine;
using System.CommandLine.Binding;

namespace Fluxzy.Cli
{
    public class ConsoleBinder : BinderBase<IConsole>
    {
        protected override IConsole GetBoundValue(BindingContext bindingContext)
        {
            return bindingContext.Console;
        }
        
    }
}