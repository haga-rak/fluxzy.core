// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Fluxzy.Utils;

namespace Fluxzy.Tools.DocGen
{
    public class DescriptionLineProvider
    {
        private static string GetPropertyFriendlyType(Type type)
        {
            if (!type.IsEnum) {
                return type.Name.ToCamelCase(); 
            }

            var enumNames = Enum.GetNames(type)
                                .Select(s => s.ToCamelCase())
                            .ToList();

            return $"{string.Join(" \\| ", enumNames)}";
        }

        public IEnumerable<FilterDescriptionLine> EnumerateFilterDescriptions(Type filterType)
        {
            var defaultInstance = ReflectionHelper.GetForcedInstance<Filter>(filterType);
            var isPremadeFilter = defaultInstance.PreMadeFilter;

            return filterType.GetProperties()
                             .Select(n => new {
                                 PropertyInfo = n,
                                 DistinctiveAttribute = n.GetCustomAttribute<FilterDistinctiveAttribute>()
                             })
                             .Where(p => p.DistinctiveAttribute != null)
                             .Where(p => !isPremadeFilter || p.PropertyInfo.Name.Equals(nameof(Filter.Inverted)))
                             .Select(n => new FilterDescriptionLine(
                                 n.PropertyInfo.Name.ToCamelCase(),
                                 GetPropertyFriendlyType(n.PropertyInfo.PropertyType),
                                 n.DistinctiveAttribute!.Description,
                                 defaultInstance?.GetType().GetProperty(n.PropertyInfo.Name)
                                                ?.GetValue(defaultInstance)?.ToString()?.ToCamelCase() ?? "*null*"
                             ));

        }

        public IEnumerable<ActionDescriptionLine> EnumerateActionDescriptions(Type filterType)
        {
            var defaultInstance = ReflectionHelper.GetForcedInstance<Rules.Action>(filterType);
            var isPremade = defaultInstance.IsPremade(); 

            return filterType.GetProperties()
                             .Select(n => new {
                                 PropertyInfo = n,
                                 DistinctiveAttribute = n.GetCustomAttribute<ActionDistinctiveAttribute>()
                             })
                             .Where(p => p.DistinctiveAttribute != null)
                             .Where(p => !isPremade)
                             .Select(n => new ActionDescriptionLine(
                                 n.PropertyInfo.Name.ToCamelCase(),
                                 GetPropertyFriendlyType(n.PropertyInfo.PropertyType),
                                 n.DistinctiveAttribute!.Description,
                                 defaultInstance?.GetType().GetProperty(n.PropertyInfo.Name)
                                                ?.GetValue(defaultInstance)?.ToString()?.ToCamelCase() ?? "*null*"
                             ));

        }
    }
}
