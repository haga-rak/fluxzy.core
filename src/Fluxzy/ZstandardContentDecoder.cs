// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using ZstdSharp;

namespace Fluxzy.Cli
{
    /// <summary>
    ///     zstd content-encoding decoder for the CLI, backed by ZstdSharp. Fluxzy.Core ships no zstd codec
    ///     by design; the CLI plugs this in through <see cref="ContentDecoderRegistry" /> at startup.
    /// </summary>
    internal sealed class ZstandardContentDecoder : IContentDecoder
    {
        public string EncodingToken => "zstd";

        // leaveOpen:false to match Fluxzy's native gzip/deflate/brotli decoders.
        public Stream GetDecodedStream(Stream compressed)
            => new DecompressionStream(compressed, leaveOpen: false);
    }
}
