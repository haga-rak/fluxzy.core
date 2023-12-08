using System;
using System.Linq;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Extensions
{
    public class ConfigureActionBuilder : IConfigureActionBuilder
    {
        private readonly FluxzySetting _setting;
        private readonly Filter _filter;

        public ConfigureActionBuilder(FluxzySetting setting, Filter filter)
        {
            _setting = setting;
            _filter = filter;
        }
        
        public IConfigureFilterBuilder Do(Action action, params Action [] actions)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _setting.AddAlterationRules(new Rule(action, _filter));
            _setting.AddAlterationRules(actions.Select(a => new Rule(a, _filter)));
            
            return new ConfigureFilterBuilderBuilder(_setting);
        }
    }
}