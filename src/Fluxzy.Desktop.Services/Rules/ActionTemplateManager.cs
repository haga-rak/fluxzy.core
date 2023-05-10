// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reflection;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Rules.Actions;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Desktop.Services.Rules
{
    public class ActionTemplateManager
    {
        private static readonly List<TypeAction> TypeActions;
        private static readonly List<Action> DefaultTemplates = new();
        private static readonly Dictionary<Type, Action> Instances = new();
        private static readonly Dictionary<string, ActionMetadataAttribute> DescriptionMapping = new(StringComparer.OrdinalIgnoreCase);

        static ActionTemplateManager()
        {
            TypeActions = typeof(Action).Assembly.GetTypes()
                                        .Where(derivedType => typeof(Action).IsAssignableFrom(derivedType)
                                                              && derivedType.IsClass
                                                              && !derivedType.IsAbstract)
                                        .Where(derivedType =>
                                            derivedType.GetCustomAttribute<ActionMetadataAttribute>() != null)

                                        // TODO : update this suboptimal double call of GetCustomAttribute
                                        .Select(derivedType => new TypeAction(derivedType,
                                            derivedType.GetCustomAttribute<ActionMetadataAttribute>()!))
                                        .ToList();

            foreach (var item in TypeActions) {
                var filter = CreateAction(item);
                Instances[item.Type] = filter;
                DefaultTemplates.Add(filter);
                DescriptionMapping[filter.TypeKind] = item.Metadata;
            }

            DefaultTemplates = DefaultTemplates.OrderBy(t => t.FriendlyName).ToList();
        }

        private static Action CreateAction(TypeAction item)
        {
            var constructor = item.Type.GetConstructors()
                                  .OrderByDescending(t => t.GetParameters().Length).First();

            var arguments = new List<object>();

            foreach (var argument in constructor.GetParameters()) {
                arguments.Add(argument.ParameterType == typeof(string)
                    ? string.Empty
                    : ReflectionHelper.GetDefault(argument.ParameterType));
            }

            var action = (Action) constructor.Invoke(arguments.ToArray());

            return action;
        }

        public bool TryGetDescription(string typeKind, out string longDescription)
        {
            if (DescriptionMapping.TryGetValue(typeKind, out var metaData)) {
                longDescription = metaData.LongDescription ?? string.Empty;

                return true;
            }

            longDescription = string.Empty;

            return false;
        }

        public List<Action> GetDefaultActions()
        {
            var res = DefaultTemplates.ToList();

            return res;
        }
    }

    internal class TypeAction
    {
        public TypeAction(Type type, ActionMetadataAttribute action)
        {
            Type = type;
            Metadata = action;
        }

        public Type Type { get; }

        public ActionMetadataAttribute Metadata { get; }
    }
}
