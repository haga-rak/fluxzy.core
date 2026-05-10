// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands.SpectreInfra
{
    internal sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly Dictionary<Type, object> _instances;
        private readonly Dictionary<Type, Type> _registrations;
        private readonly Dictionary<Type, Func<object>> _lazies;

        public TypeResolver(
            Dictionary<Type, object> instances,
            Dictionary<Type, Type> registrations,
            Dictionary<Type, Func<object>> lazies)
        {
            _instances = instances;
            _registrations = registrations;
            _lazies = lazies;
        }

        public object? Resolve(Type? type)
        {
            if (type == null) {
                return null;
            }

            if (_instances.TryGetValue(type, out var instance)) {
                return instance;
            }

            if (_lazies.TryGetValue(type, out var factory)) {
                return factory();
            }

            if (_registrations.TryGetValue(type, out var implementation)) {
                return CreateInstance(implementation);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                var elementType = type.GetGenericArguments()[0];

                return Array.CreateInstance(elementType, 0);
            }

            return null;
        }

        private object? CreateInstance(Type type)
        {
            var ctor = type.GetConstructors()
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault();

            if (ctor == null) {
                return Activator.CreateInstance(type);
            }

            var parameters = ctor.GetParameters();

            if (parameters.Length == 0) {
                return Activator.CreateInstance(type);
            }

            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++) {
                args[i] = Resolve(parameters[i].ParameterType);
            }

            return ctor.Invoke(args);
        }

        public void Dispose()
        {
            foreach (var instance in _instances.Values) {
                if (instance is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }
    }
}
