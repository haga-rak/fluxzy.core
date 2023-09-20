// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Set a variable or update an existing", NonDesktopAction = true)]
    public class SetVariableAction : Action
    {
        public SetVariableAction(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [ActionDistinctive(Description = "Variable name")]
        public string Name { get; set; }

        [ActionDistinctive(Description = "Variable value")]
        public string Value { get; set; }

        [ActionDistinctive(Description = "The scope where the variable is evaluated", 
            DefaultValue = nameof(FilterScope.RequestHeaderReceivedFromClient))]

        public FilterScope Scope { get; set; } = FilterScope.RequestHeaderReceivedFromClient;

        public override FilterScope ActionScope => Scope;

        public override string DefaultDescription => "Set variable"; 

        public override string Description => "Set variable"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return default; 

            var actualValue = Value.EvaluateVariable(context);

            if (actualValue == null) {
                context.VariableContext.Unset(Name);
                return default;
            }
            
            context.VariableContext.Set($"captured.{Name}", actualValue);
            return default;
        }
    }
}
