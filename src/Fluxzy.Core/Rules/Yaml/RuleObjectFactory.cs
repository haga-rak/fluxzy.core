// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization.ObjectFactories;

namespace Fluxzy.Rules.Yaml
{
    /// <summary>
    ///     Creates rule model types that lack a parameterless constructor by invoking the smallest
    ///     constructor with placeholder arguments; the deserializer then binds the real values.
    /// </summary>
    internal sealed class RuleObjectFactory : DefaultObjectFactory
    {
        public override object Create(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) != null) {
                return base.Create(type);
            }

            var constructor = type.GetConstructors()
                                  .OrderBy(c => c.GetParameters().Length)
                                  .FirstOrDefault();

            if (constructor == null) {
                return RuntimeHelpers.GetUninitializedObject(type);
            }

            var arguments = constructor.GetParameters()
                                       .Select(GetPlaceholderArgument)
                                       .ToArray();

            try {
                return constructor.Invoke(arguments);
            }
            catch {
                // Constructor rejected the placeholder arguments; fall back to an uninitialized instance.
                return RuntimeHelpers.GetUninitializedObject(type);
            }
        }

        private static object? GetPlaceholderArgument(ParameterInfo parameter)
        {
            var parameterType = parameter.ParameterType;

            if (parameterType.IsArray) {
                // Empty array for params arrays so the constructor body does not dereference null.
                return Array.CreateInstance(parameterType.GetElementType()!, 0);
            }

            if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null) {
                return Activator.CreateInstance(parameterType);
            }

            return null;
        }
    }
}
