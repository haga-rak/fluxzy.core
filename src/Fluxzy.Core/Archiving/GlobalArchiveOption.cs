// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Text.Json.Serialization;
using Fluxzy.Archiving.MessagePack;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Fluxzy
{
    public static class GlobalArchiveOption
    {
        /// <summary>
        /// Storage serializer 
        /// </summary>
        public static MessagePackSerializerOptions MessagePackSerializerOptions { get; } = new(
            CompositeResolver.Create(new IMessagePackFormatter[] { new MessagePackAddressFormatter() },
            new IFormatterResolver[] { StandardResolverAllowPrivate.Instance, ContractlessStandardResolver.Instance }));

        public static JsonSerializerOptions DefaultSerializerOptions { get; } = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = {
                new ReadonlyMemoryCharConverter(),
                new BooleanConverter(),
                new JsonStringEnumConverter(),
                new IpAddressConverter(),
                new IpEndPointConverter(),
                new PolymorphicConverter<Filter>(),
                new PolymorphicConverter<Action>()
            },
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static JsonSerializerOptions ConfigSerializerOptions { get; } = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = false,
            Converters = {
                new ReadonlyMemoryCharConverter(),
                new BooleanConverter(),
                new JsonStringEnumConverter(),
                new IpAddressConverter(),
                new IpEndPointConverter(),
                new PolymorphicConverter<Filter>(),
                new PolymorphicConverter<Action>()
            },
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static JsonSerializerOptions HttpArchiveSerializerOptions { get; } = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new ReadonlyMemoryCharConverter(),
                new BooleanConverter(),
                new JsonStringEnumConverter()
            }
        };
    }
}
