// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules
{
    public abstract class Action : PolymorphicObject
    {
        [YamlIgnore]
        public abstract FilterScope ActionScope { get; }

        [YamlIgnore]
        public int ScopeId => (int) ActionScope;

        [YamlIgnore]
        public virtual Guid Identifier { get; set; } = Guid.NewGuid();

        [YamlIgnore]
        public abstract string DefaultDescription { get; }

        public virtual string? Description { get; set; }

        [YamlIgnore]
        public virtual string FriendlyName =>
            !string.IsNullOrWhiteSpace(Description) ? Description : DefaultDescription;

        protected override string Suffix { get; } = nameof(Action);

        [YamlIgnore]
        public bool NoEditableSetting {
            get
            {
                // Only ActionDistinctiveAttribute can be editable setting
                return 
                    GetType().GetProperties()
                             .Count(p => p.GetCustomAttribute<ActionDistinctiveAttribute>() != null) == 0;
            }
        }

        /// <summary>
        /// Called once by the engine to initialize this directive
        /// </summary>
        /// <param name="startupContext"></param>
        public virtual void Init(StartupContext startupContext)
        {

        }

        public ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            return InternalAlter(context, exchange, connection, scope, breakPointManager);
        }

        public abstract ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager);

        public bool IsPremade()
        {
            // Check for default constructor 

            var type = GetType();
            var defaultConstructor = type.GetConstructor(Type.EmptyTypes);

            if (defaultConstructor == null)
                return false;

            return true;
        }

        public virtual IEnumerable<ActionExample> GetExamples()
        {
            var type = GetType();

            var actionMetaDataAttribute = type.GetCustomAttribute<ActionMetadataAttribute>();

            if (actionMetaDataAttribute == null)
                yield break;

            if (!IsPremade())
                yield break;

            var description = actionMetaDataAttribute.LongDescription;

            yield return new ActionExample(description, this);
        }

        /// <summary>
        /// Check if the action is valid for the given setting and filter
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="filter"></param>
        /// <returns>A list of validation result items</returns>
        public virtual IEnumerable<ValidationResult> Validate(FluxzySetting setting, Filter filter)
        {
#if NET6_0_OR_GREATER
            var targets = GetType().GetProperties()
                                      .Select(s => new {
                                          Property = s,
                                          ActionDistinctiveAttribute = s.GetCustomAttribute<ActionDistinctiveAttribute>()
                                      })
                                      .Where(w => w.ActionDistinctiveAttribute != null)
                                      .ToList();

            foreach (var target in targets)
            {
                var isNullable = new NullabilityInfoContext().Create(target.Property
                    ).WriteState is NullabilityState.Nullable;

                var value = target.Property.GetValue(this);

                if (value == null && !isNullable)
                {
                    yield return new ValidationResult(
                        ValidationRuleLevel.Error,
                        $"The property {target.Property.Name} is required for this action (`{FriendlyName}`).",
                        GetType().Name
                    );
                }
            }
#else
            yield break;
#endif

        }
    }
}
