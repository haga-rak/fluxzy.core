// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using Fluxzy.Archiving.Har;
using Fluxzy.Archiving.Saz;
using Fluxzy.Certificates;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Extensions;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy
{
    [JsonSerializable(typeof(StringSelectorOperation))]
    [JsonSerializable(typeof(SelectorCollectionOperation))]
    [JsonSerializable(typeof(HttpArchiveSavingBodyPolicy))]
    [JsonSerializable(typeof(SazFlags))]
    [JsonSerializable(typeof(CompressionType))]
    [JsonSerializable(typeof(PackableFileType))]
    [JsonSerializable(typeof(ArchiveUpdateType))]
    [JsonSerializable(typeof(ArchivingPolicyType))]
    [JsonSerializable(typeof(CertificateRetrieveMode))]
    [JsonSerializable(typeof(SslProvider))]
    [JsonSerializable(typeof(WsOpCode))]
    [JsonSerializable(typeof(WsMessageDirection))]
    [JsonSerializable(typeof(H2ErrorCode))]
    [JsonSerializable(typeof(BodyContentLoadingType))]
    [JsonSerializable(typeof(BodyType))]
    [JsonSerializable(typeof(BreakPointStatus))]
    [JsonSerializable(typeof(BreakPointLocation))]
    [JsonSerializable(typeof(ExchangeStep))]
    [JsonSerializable(typeof(FilterScope))]
    [JsonSerializable(typeof(object))]
    public partial class FluxzyJsonSerializerContext : JsonSerializerContext { }
}
