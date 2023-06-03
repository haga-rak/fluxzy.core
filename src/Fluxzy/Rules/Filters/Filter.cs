// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Reflection;
using Fluxzy.Core;
using Fluxzy.Misc.Converters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Filters
{
    public abstract class Filter : PolymorphicObject
    {
        [YamlIgnore]
        public virtual Guid Identifier => this.BuildDistinctiveIdentifier();

        [FilterDistinctive(Description = "Negate the filter result")]
        public bool Inverted { get; set; }

        [YamlIgnore]
        public abstract FilterScope FilterScope { get; }

        [YamlIgnore]
        public int ScopeId => (int) FilterScope;

        [YamlIgnore]
        public virtual string AutoGeneratedName { get; } = "Filter";

        [YamlIgnore]
        public virtual bool PreMadeFilter { get; } = false;

        [YamlIgnore]
        public virtual string GenericName => string.Empty;

        public bool Locked { get; set; }

        [YamlIgnore]
        public virtual string? ShortName { get; } = "custom";
        
        public virtual string? Description { get; set; }

        [YamlIgnore]
        public string FriendlyName {
            get
            {
                var initialName = !string.IsNullOrWhiteSpace(Description) ? Description : AutoGeneratedName;

                if (Inverted)
                    initialName = $"NOT {initialName}";

                return initialName;
            }
        }

        [YamlIgnore]
        public string Category {
            get
            {
                var typeFullName = GetType().FullName!;

                if (typeFullName.Contains("RequestFilters"))
                    return "Request";

                if (typeFullName.Contains("ResponseFilters"))
                    return "Response";

                return "Global";
            }
        }

        [YamlIgnore]
        public virtual bool Common { get; set; } = false;

        protected override string Suffix { get; } = nameof(Filter);

        protected abstract bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext);

        public virtual bool Apply(ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            var internalApplyResult = InternalApply(exchangeContext, authority, exchange, filteringContext);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }

        public abstract IEnumerable<FilterExample> GetExamples();


        protected virtual FilterExample? GetDefaultSample()
        {
            var type = this.GetType();
            var filterMetaData = type.GetCustomAttribute<FilterMetaDataAttribute>();

            if (filterMetaData == null)
                return null;

            var description = filterMetaData.LongDescription;

            if (string.IsNullOrWhiteSpace(description))
                return null;

            return new FilterExample(description, this);
        }
    }


    public class FilterExample
    {
        public FilterExample(string description, Filter filter)
        {
            Description = description;
            Filter = filter;
        }

        public string Description { get; } 

        public Filter Filter { get;  }

        public string ? ExtraNote { get; set; }
    }
}
