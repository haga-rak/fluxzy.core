using System;
using System.Linq;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Extensions
{
    internal class ConfigureActionBuilder : IConfigureActionBuilder
    {
        public FluxzySetting Setting { get; }

        private readonly Filter _filter;

        public ConfigureActionBuilder(FluxzySetting setting, Filter filter)
        {
            Setting = setting;
            _filter = filter;
        }
        
        public IConfigureFilterBuilder Do(Action action, params Action [] actions)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Setting.AddAlterationRules(new Rule(action, _filter));
            Setting.AddAlterationRules(actions.Select(a => new Rule(a, _filter)));
            
            return new ConfigureFilterBuilderBuilder(Setting);
        }
    }
}