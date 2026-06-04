// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy
{
    /// <summary>
    ///     Decodes an HTTP content-encoding that the .NET runtime cannot handle natively.
    ///     gzip, deflate and brotli are decoded by Fluxzy out of the box; any other encoding
    ///     (e.g. <c>compress</c>/LZW or <c>zstd</c>) requires a decoder to be registered through
    ///     <see cref="ContentDecoderRegistry" />.
    /// </summary>
    /// <example>
    ///     Using a delegate:
    ///     <code>
    ///     ContentDecoderRegistry.Register("zstd", compressed => new MyZstdDecodeStream(compressed));
    ///     </code>
    /// </example>
    public interface IContentDecoder
    {
        /// <summary>
        ///     The content-encoding token this decoder handles, lowercase (e.g. <c>"zstd"</c>, <c>"compress"</c>).
        /// </summary>
        string EncodingToken { get; }

        /// <summary>
        ///     Wraps a compressed input stream and returns a stream yielding the decoded bytes.
        /// </summary>
        /// <param name="compressed">The raw, still-encoded stream.</param>
        /// <returns>A readable stream producing the decoded content.</returns>
        Stream GetDecodedStream(Stream compressed);
    }

    /// <summary>
    ///     Factory methods for creating <see cref="IContentDecoder" /> instances.
    /// </summary>
    public static class ContentDecoder
    {
        /// <summary>
        ///     Creates an <see cref="IContentDecoder" /> from a delegate.
        /// </summary>
        /// <param name="encodingToken">The content-encoding token handled (e.g. <c>"zstd"</c>).</param>
        /// <param name="decode">A function wrapping a compressed stream into a decoding stream.</param>
        public static IContentDecoder Create(string encodingToken, Func<Stream, Stream> decode)
        {
            if (string.IsNullOrWhiteSpace(encodingToken))
                throw new ArgumentException("Encoding token must not be empty", nameof(encodingToken));

            return new DelegateContentDecoder(encodingToken,
                decode ?? throw new ArgumentNullException(nameof(decode)));
        }

        private class DelegateContentDecoder : IContentDecoder
        {
            private readonly Func<Stream, Stream> _decode;

            public DelegateContentDecoder(string encodingToken, Func<Stream, Stream> decode)
            {
                EncodingToken = encodingToken;
                _decode = decode;
            }

            public string EncodingToken { get; }

            public Stream GetDecodedStream(Stream compressed) => _decode(compressed);
        }
    }
}
