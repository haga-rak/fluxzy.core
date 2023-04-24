﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Text.Json.Serialization;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public static class GlobalArchiveOption
    {
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