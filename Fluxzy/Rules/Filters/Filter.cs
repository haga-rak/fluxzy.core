﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc.Converters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Filters
{
    public abstract class Filter : PolymorphicObject
    {
        [YamlIgnore]
        public virtual Guid Identifier { get; init; } = Guid.NewGuid();

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
                var typeFullName = GetType().FullName;

                if (typeFullName.Contains("RequestFilters"))
                    return "Request";

                if (typeFullName.Contains("ResponseFilters"))
                    return "Response";

                return "Global";
            }
        }

        [YamlIgnore]
        public virtual bool Common { get; set; } = false;

        protected abstract bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext);

        public virtual bool Apply(IAuthority authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            var internalApplyResult = InternalApply(authority, exchange, filteringContext);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }
    }
}
