// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Yaml
{
    /// <summary>
    ///     Deserializes the rule model types that cannot be populated through setters alone: the
    ///     polymorphic <see cref="Filter" />/<see cref="Action" /> hierarchy (resolved from the
    ///     <c>typeKind</c> discriminator) and immutable value types such as <c>Tag</c>. It buffers the
    ///     node, resolves the concrete type, then binds keys to constructor parameters and public setters.
    ///     Errors are positioned <see cref="YamlException" />. Other types fall through to the standard
    ///     deserializer.
    /// </summary>
    internal sealed class RuleNodeDeserializer : INodeDeserializer
    {
        private static readonly Assembly ModelAssembly = typeof(PolymorphicObject).Assembly;
        private static readonly ConcurrentDictionary<Type, bool> ConstructorBindingCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

        public bool Deserialize(
            IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer,
            out object? value, ObjectDeserializer rootDeserializer)
        {
            var polymorphic = expectedType == typeof(Filter) || expectedType == typeof(Fluxzy.Rules.Action);

            if (!polymorphic && !NeedsConstructorBinding(expectedType)) {
                value = null;

                return false;
            }

            if (reader.Current is not MappingStart) {
                // Not an object (null or scalar); let the standard pipeline handle it.
                value = null;

                return false;
            }

            var events = BufferNode(reader);

            Type concreteType;

            if (polymorphic) {
                var (typeKind, marker) = FindTypeKind(events);

                if (typeKind == null) {
                    throw new YamlException(events[0].Start, events[0].End,
                        $"Missing 'typeKind' to resolve a {expectedType.Name}.");
                }

                if (!PolymorphicTypeResolver.TryResolve(expectedType, typeKind, out var resolved)) {
                    throw new YamlException(marker!.Start, marker.End,
                        $"Cannot resolve '{typeKind}' to a valid {expectedType.Name}.");
                }

                concreteType = resolved!;
            }
            else {
                concreteType = expectedType;
            }

            value = Bind(concreteType, events, nestedObjectDeserializer);

            return true;
        }

        private static object Bind(Type type, IReadOnlyList<ParsingEvent> events, Func<IParser, Type, object?> nested)
        {
            var parser = new EventBufferParser(events);
            parser.MoveNext();
            parser.Consume<MappingStart>();

            var properties = SerializableProperties(type);

            // Prefer the parameterless constructor; fall back to the greediest one for immutable types.
            var constructor = type.GetConstructor(Type.EmptyTypes)
                              ?? type.GetConstructors()
                                     .OrderByDescending(c => c.GetParameters().Length)
                                     .First();

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            while (parser.Current is not MappingEnd) {
                var key = parser.Consume<Scalar>().Value;

                if (string.Equals(key, "typeKind", StringComparison.OrdinalIgnoreCase)) {
                    parser.SkipThisAndNestedEvents();

                    continue;
                }

                var memberType =
                    properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                              ?.PropertyType
                    ?? constructor.GetParameters()
                                  .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                                  ?.ParameterType;

                if (memberType == null) {
                    parser.SkipThisAndNestedEvents();

                    continue;
                }

                values[key] = nested(parser, memberType);
            }

            var arguments = constructor.GetParameters()
                                       .Select(p => values.TryGetValue(p.Name!, out var value)
                                           ? value
                                           : p.ParameterType.IsValueType
                                               ? Activator.CreateInstance(p.ParameterType)
                                               : null)
                                       .ToArray();

            var instance = constructor.Invoke(arguments);

            foreach (var property in properties) {
                if (property.SetMethod is { IsPublic: true } && values.TryGetValue(property.Name, out var value)) {
                    property.SetValue(instance, value);
                }
            }

            return instance;
        }

        private static List<ParsingEvent> BufferNode(IParser reader)
        {
            var events = new List<ParsingEvent>();
            var depth = 0;

            do {
                var current = reader.Current!;
                events.Add(current);

                if (current is MappingStart or SequenceStart) {
                    depth++;
                }
                else if (current is MappingEnd or SequenceEnd) {
                    depth--;
                }

                reader.MoveNext();
            } while (depth > 0);

            return events;
        }

        private static (string? TypeKind, Scalar? Marker) FindTypeKind(IReadOnlyList<ParsingEvent> events)
        {
            var depth = 0;

            for (var i = 0; i < events.Count; i++) {
                var current = events[i];

                if (current is MappingStart or SequenceStart) {
                    depth++;
                }
                else if (current is MappingEnd or SequenceEnd) {
                    depth--;
                }
                else if (depth == 1
                         && current is Scalar { Value: "typeKind" }
                         && i + 1 < events.Count
                         && events[i + 1] is Scalar valueScalar) {
                    return (valueScalar.Value, valueScalar);
                }
            }

            return (null, null);
        }

        private static bool NeedsConstructorBinding(Type type)
        {
            return ConstructorBindingCache.GetOrAdd(type, static t =>
                t.IsClass
                && !t.IsAbstract
                && t.Assembly == ModelAssembly
                && !typeof(PolymorphicObject).IsAssignableFrom(t)
                && t.GetConstructor(Type.EmptyTypes) == null
                && t.GetConstructors().Length > 0
                && SerializableProperties(t).Any(p => p.SetMethod is not { IsPublic: true }));
        }

        private static PropertyInfo[] SerializableProperties(Type type)
        {
            return PropertyCache.GetOrAdd(type, static t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Where(p => p.GetMethod is { IsPublic: true }
                             && p.GetCustomAttribute<YamlIgnoreAttribute>() == null)
                 .ToArray());
        }
    }
}
