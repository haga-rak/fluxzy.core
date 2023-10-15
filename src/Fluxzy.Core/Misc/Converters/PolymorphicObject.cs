// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Fluxzy.Misc.Converters
{
    public abstract class PolymorphicObject
    {
        [JsonIgnore]
        [YamlIgnore]
        protected abstract string Suffix { get; }

        public string TypeKind => GetType().Name;
    }
}
