// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Fluxzy.Rules
{
    internal class SortedTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector _innerTypeInspector;

        public SortedTypeInspector(ITypeInspector innerTypeInspector)
        {
            _innerTypeInspector = innerTypeInspector;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        {
            var properties = _innerTypeInspector.GetProperties(type, container);

            return properties.OrderByDescending(x => x.Name == "typeKind");
        }

        public override string GetEnumName(Type enumType, string name)
            => _innerTypeInspector.GetEnumName(enumType, name);

        public override string GetEnumValue(object enumValue)
            => _innerTypeInspector.GetEnumValue(enumValue);

        public override bool HasParseMethod(Type type)
            => _innerTypeInspector.HasParseMethod(type);

        public override object? Parse(string value, Type expectedType)
            => _innerTypeInspector.Parse(value, expectedType);
    }
}
