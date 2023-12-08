using System;
using System.Linq;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Extensions
{
    internal class ConfigureFilterBuilderBuilder : IConfigureFilterBuilder
    {
        public ConfigureFilterBuilderBuilder(FluxzySetting fluxzySetting)
        {
            FluxzySetting = fluxzySetting;
        }

        public FluxzySetting FluxzySetting { get; }

        public IConfigureActionBuilder When(Filter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return new ConfigureActionBuilder(FluxzySetting, filter);
        }

        public IConfigureActionBuilder WhenAny(params Filter[] filters)
        {
            return new ConfigureActionBuilder(FluxzySetting,
                filters.Any() ? new FilterCollection(filters) { Operation = SelectorCollectionOperation.Or }: AnyFilter.Default);
        }

        public IConfigureActionBuilder WhenAll(params Filter[] filters)
        {
            return new ConfigureActionBuilder(FluxzySetting, filters.Any() ? 
                new FilterCollection(filters) { Operation = SelectorCollectionOperation.And } 
                : NoFilter.Default);
        }
    }
}