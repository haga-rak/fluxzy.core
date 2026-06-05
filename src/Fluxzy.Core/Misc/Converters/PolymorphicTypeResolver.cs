// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fluxzy.Misc.Converters
{
    /// <summary>
    ///     Maps a <c>typeKind</c> to a concrete <see cref="PolymorphicObject" /> type by scanning the
    ///     declaring assembly. Shared by the System.Text.Json archive path and the YAML rule reader.
    /// </summary>
    internal static class PolymorphicTypeResolver
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Type>> Maps = new();

        public static IReadOnlyDictionary<string, Type> GetMap(Type baseType)
        {
            return Maps.GetOrAdd(baseType, Build);
        }

        /// <summary>
        ///     Resolves a <c>typeKind</c> to a concrete type: exact class name, then a
        ///     <c>typeKind + baseTypeName</c> fallback (so <c>Host</c> resolves to <c>HostFilter</c>).
        /// </summary>
        public static bool TryResolve(Type baseType, string typeKind, out Type? type)
        {
            var map = GetMap(baseType);

            return map.TryGetValue(typeKind, out type)
                   || map.TryGetValue(typeKind + baseType.Name, out type);
        }

        private static IReadOnlyDictionary<string, Type> Build(Type baseType)
        {
            var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in baseType.Assembly.GetTypes()) {
                if (baseType.IsAssignableFrom(type)
                    && type != baseType
                    && !type.IsAbstract
                    && type.IsClass) {
                    map[type.Name] = type;
                }
            }

            return map;
        }
    }
}
