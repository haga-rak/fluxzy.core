// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Converters;
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
    }
}
