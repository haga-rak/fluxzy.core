// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Fluxzy
{
    /// <summary>
    ///     A process-wide registry of <see cref="IContentDecoder" /> instances allowing callers to plug in
    ///     decoders for HTTP content-encodings the .NET runtime cannot decode natively (e.g. <c>compress</c>/LZW,
    ///     <c>zstd</c>).
    ///     gzip, deflate and brotli are always handled by Fluxzy and do not need to be registered.
    /// </summary>
    /// <remarks>
    ///     The goal of this extension point is to keep <c>Fluxzy.Core</c> free of extra third-party dependencies:
    ///     a caller that needs an additional encoding brings the decoding library of their choice and registers
    ///     it here. When Fluxzy encounters an encoding with no registered decoder it throws a
    ///     <see cref="FluxzyException" /> rather than silently leaving the body encoded.
    /// </remarks>
    public static class ContentDecoderRegistry
    {
        private static readonly ConcurrentDictionary<string, IContentDecoder> Decoders =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers (or replaces) a decoder for its <see cref="IContentDecoder.EncodingToken" />.
        /// </summary>
        public static void Register(IContentDecoder decoder)
        {
            if (decoder == null)
                throw new ArgumentNullException(nameof(decoder));

            if (string.IsNullOrWhiteSpace(decoder.EncodingToken))
                throw new ArgumentException("Decoder encoding token must not be empty", nameof(decoder));

            Decoders[decoder.EncodingToken] = decoder;
        }

        /// <summary>
        ///     Registers (or replaces) a decoder for <paramref name="encodingToken" /> from a delegate.
        /// </summary>
        public static void Register(string encodingToken, Func<Stream, Stream> factory)
        {
            Register(ContentDecoder.Create(encodingToken, factory));
        }

        /// <summary>
        ///     Attempts to resolve a registered decoder for <paramref name="encodingToken" /> (case-insensitive).
        /// </summary>
        public static bool TryGet(string encodingToken, out IContentDecoder decoder)
        {
            if (string.IsNullOrEmpty(encodingToken)) {
                decoder = null!;

                return false;
            }

            return Decoders.TryGetValue(encodingToken, out decoder!);
        }

        /// <summary>
        ///     Returns true if a decoder is registered for <paramref name="encodingToken" /> (case-insensitive).
        /// </summary>
        public static bool Contains(string encodingToken)
        {
            return !string.IsNullOrEmpty(encodingToken) && Decoders.ContainsKey(encodingToken);
        }

        /// <summary>
        ///     Removes the decoder registered for <paramref name="encodingToken" />, if any.
        /// </summary>
        public static bool Unregister(string encodingToken)
        {
            return !string.IsNullOrEmpty(encodingToken) && Decoders.TryRemove(encodingToken, out _);
        }
    }
}
