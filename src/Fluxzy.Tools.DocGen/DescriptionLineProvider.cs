// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Fluxzy.Utils;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tools.DocGen
{
    public class DescriptionLineProvider
    {
        private static string GetPropertyFriendlyType(Type type)
        {
            if (type == typeof(int?)) {
            }

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
                             .Select(s => new PropertyDescription(s.PropertyInfo, s.DistinctiveAttribute!))
                             .Where(p => !isPremadeFilter || p.PropertyInfo.Name.Equals(nameof(Filter.Inverted)))
                             .Expand()
                             .Select(n => new FilterDescriptionLine(
                                 n.FullName,
                                 n.DistinctiveAttribute.FriendlyType ??
                                 GetPropertyFriendlyType(n.PropertyInfo.PropertyType),
                                 n.DistinctiveAttribute!.Description,
                                 n.DistinctiveAttribute.DefaultValue ?? defaultInstance?.GetType()
                                     .GetProperty(n.PropertyInfo.Name)
                                     ?.GetValue(defaultInstance)?.ToString()?.ToCamelCase() ?? "*null*"
                             ));
        }

        public IEnumerable<ActionDescriptionLine> EnumerateActionDescriptions(Type filterType)
        {
            var defaultInstance = ReflectionHelper.GetForcedInstance<Action>(filterType);
            var isPremade = defaultInstance.IsPremade();

            return filterType.GetProperties()
                             .Select(n => new {
                                 PropertyInfo = n,
                                 DistinctiveAttribute = n.GetCustomAttribute<ActionDistinctiveAttribute>()
                             })
                             .Where(p => p.DistinctiveAttribute != null)
                             .Select(s => new PropertyDescription(s.PropertyInfo, s.DistinctiveAttribute!))
                             .Where(p => !isPremade)
                             .Expand()
                             .Select(n => new ActionDescriptionLine(
                                 n.FullName,
                                 n.DistinctiveAttribute.FriendlyType ??
                                 GetPropertyFriendlyType(n.PropertyInfo.PropertyType),
                                 n.DistinctiveAttribute!.Description,
                                 defaultInstance?.GetType().GetProperty(n.PropertyInfo.Name)
                                                ?.GetValue(defaultInstance)?.ToString()?.ToCamelCase() ?? ""
                             ));
        }
    }

    internal class PropertyDescription
    {
        public PropertyDescription(PropertyInfo propertyInfo, PropertyDistinctiveAttribute distinctiveAttribute)
        {
            PropertyInfo = propertyInfo;
            DistinctiveAttribute = distinctiveAttribute;
        }

        public PropertyInfo PropertyInfo { get; }

        public PropertyDistinctiveAttribute DistinctiveAttribute { get; }

        public PropertyDescription? Parent { get; set; }

        public string FullName {
            get
            {
                var ancestorNames = GetAncestors()
                                    .Select(n => n.PropertyInfo.Name.ToCamelCase())
                                    .ToList();

                ancestorNames.Reverse();
                ancestorNames.Add(PropertyInfo.Name.ToCamelCase());

                return $"{string.Join(".", ancestorNames)}";
            }
        }

        private IEnumerable<PropertyDescription> GetAncestors()
        {
            if (Parent == null) {
                yield break;
            }

            yield return Parent;

            foreach (var ancestor in Parent.GetAncestors()) {
                yield return ancestor;
            }
        }
    }

    internal static class PropertyHelper
    {
        internal static IEnumerable<PropertyDescription> Expand(this IEnumerable<PropertyDescription> items)
        {
            foreach (var item in items) {
                if (!item.DistinctiveAttribute.Expand) {
                    yield return item;

                    continue;
                }

                var subProperties = item.PropertyInfo
                                        .PropertyType.GetProperties()
                                        .Select(n => new {
                                            PropertyInfo = n,
                                            DistinctiveAttribute = n.GetCustomAttribute<PropertyDistinctiveAttribute>()
                                        })
                                        .Where(p => p.DistinctiveAttribute != null)
                                        .Select(
                                            s => new PropertyDescription(s.PropertyInfo, s.DistinctiveAttribute!) {
                                                Parent = item
                                            });

                foreach (var subProperty in subProperties.Expand()) {
                    yield return subProperty;
                }
            }
        }
    }
}
