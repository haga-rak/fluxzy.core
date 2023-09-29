// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Misc;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public static class FilterDistinctiveAttributeLoader
    {
        private static readonly Dictionary<string, PropertyInfo[]> ExistingProperties = new();

        public static Guid BuildDistinctiveIdentifier(this Filter filter)
        {
            var filterType = filter.GetType()!;

            if (!ExistingProperties.TryGetValue(filterType.FullName!, out var properties))
            {
                properties = filter.GetType().GetProperties()
                                   .Select(p => new
                                   {
                                       Attribute = p.GetCustomAttribute<FilterDistinctiveAttribute>(true),
                                       Property = p
                                   })
                                   .Where(p => p.Attribute != null)
                                   .Select(p => p.Property)
                                   .OrderBy(p => p.Name)
                                   .ToArray();


                ExistingProperties[
                        filterType.FullName ?? throw new InvalidOperationException("filterType cannot be null")] =
                    properties;
            }

            var identifier = filterType.Name + string.Join("",
                properties.Select(p => p.GetValue(filter)?.ToString() ?? string.Empty));

            return identifier.GetMd5Guid();
        }
    }
}
