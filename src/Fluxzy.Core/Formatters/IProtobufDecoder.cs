// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Formatters
{
    /// <summary>
    ///     Decodes raw protobuf message bytes into a human-readable text representation.
    ///     Implement this interface to provide custom protobuf decoding
    ///     without requiring the protoc CLI tool on PATH.
    /// </summary>
    /// <example>
    ///     Using a delegate:
    ///     <code>
    ///     formatSettings.ProtobufDecoder = ProtobufDecoder.Create(context => {
    ///         // Your custom decoding logic here
    ///         return decodedText;
    ///     });
    ///     </code>
    /// </example>
    public interface IProtobufDecoder
    {
        /// <summary>
        ///     Attempts to decode a protobuf message into a human-readable text representation.
        /// </summary>
        /// <param name="context">Context containing the raw message bytes and gRPC metadata.</param>
        /// <returns>A decoded text representation of the message, or null if decoding is not possible.</returns>
        string? TryDecode(ProtobufDecodeContext context);
    }

    /// <summary>
    ///     Provides context for decoding a single protobuf message extracted from a gRPC frame.
    /// </summary>
    public readonly struct ProtobufDecodeContext
    {
        public ProtobufDecodeContext(
            ReadOnlyMemory<byte> messageData,
            string? serviceName,
            string? methodName,
            bool isRequest)
        {
            MessageData = messageData;
            ServiceName = serviceName;
            MethodName = methodName;
            IsRequest = isRequest;
        }

        /// <summary>
        ///     The raw protobuf message bytes (without the gRPC 5-byte frame header).
        /// </summary>
        public ReadOnlyMemory<byte> MessageData { get; }

        /// <summary>
        ///     The gRPC service name extracted from the request path (e.g., "mypackage.MyService").
        /// </summary>
        public string? ServiceName { get; }

        /// <summary>
        ///     The gRPC method name extracted from the request path (e.g., "MyMethod").
        /// </summary>
        public string? MethodName { get; }

        /// <summary>
        ///     True if this is a request (input) message, false for a response (output) message.
        /// </summary>
        public bool IsRequest { get; }
    }

    /// <summary>
    ///     Factory methods for creating <see cref="IProtobufDecoder" /> instances.
    /// </summary>
    public static class ProtobufDecoder
    {
        /// <summary>
        ///     Creates an <see cref="IProtobufDecoder" /> from a delegate.
        /// </summary>
        /// <param name="decode">
        ///     A function that takes a <see cref="ProtobufDecodeContext" /> and returns
        ///     a decoded text representation, or null if decoding is not possible.
        /// </param>
        public static IProtobufDecoder Create(Func<ProtobufDecodeContext, string?> decode)
        {
            return new DelegateProtobufDecoder(decode ?? throw new ArgumentNullException(nameof(decode)));
        }

        private class DelegateProtobufDecoder : IProtobufDecoder
        {
            private readonly Func<ProtobufDecodeContext, string?> _decode;

            public DelegateProtobufDecoder(Func<ProtobufDecodeContext, string?> decode)
            {
                _decode = decode;
            }

            public string? TryDecode(ProtobufDecodeContext context) => _decode(context);
        }
    }
}
