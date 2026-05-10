// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands.SpectreInfra
{
    internal sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly Dictionary<Type, object> _instances = new();
        private readonly Dictionary<Type, Type> _registrations = new();
        private readonly Dictionary<Type, Func<object>> _lazies = new();

        public void Register(Type service, Type implementation)
        {
            _registrations[service] = implementation;
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _instances[service] = implementation;
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            _lazies[service] = factory;
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(_instances, _registrations, _lazies);
        }
    }
}
