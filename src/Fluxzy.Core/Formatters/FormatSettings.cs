// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Formatters
{
    public class FormatSettings
    {
        public static FormatSettings Default { get; } = new();

        public int MaxFormattableJsonLength { get; set; } = 2 * 1024 * 1024;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 1024;

        public int MaxHeaderLength { get; set; } = 1024 * 48;

        public int MaxMultipartContentStringLength { get; set; } = 1024;

        public int MaximumRenderableBodyLength { get; set; } = 4 * 1024 * 1024;

        public int MaxFormattableProtobufLength { get; set; } = 2 * 1024 * 1024;

        /// <summary>
        ///     An optional custom protobuf decoder. When set, this decoder is tried first for
        ///     gRPC message decoding. If it returns null, the built-in protoc-based decoding
        ///     is used as a fallback (when <see cref="ProtoDirectories" /> is configured and
        ///     protoc is available on PATH), followed by raw wire-format decoding.
        /// </summary>
        public IProtobufDecoder? ProtobufDecoder { get; set; }

        public List<string> ProtoDirectories { get; set; } = new();
    }
}
