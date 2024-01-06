// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.H2.Encoder
{
    public class HPackEncoder : IDisposable
    {
        private readonly CodecSetting _codecSetting;
        private readonly ArrayPoolMemoryProvider<char> _memoryProvider;
        private readonly PrimitiveOperation _primitiveOperation;

        /// <summary>
        ///     Decoding process
        /// </summary>
        /// <param name="encodingContext"></param>
        /// <param name="primitiveOperation"></param>
        /// <param name="codecSetting"></param>
        /// <param name="memoryProvider"></param>
        /// <param name="parser"></param>
        /// <exception cref="HPackCodecException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal HPackEncoder(
            EncodingContext encodingContext,
            CodecSetting? codecSetting = null,
            ArrayPoolMemoryProvider<char>? memoryProvider = null,
            PrimitiveOperation? primitiveOperation = null)
        {
            Context = encodingContext;
            _primitiveOperation = primitiveOperation ?? new PrimitiveOperation(new HuffmanCodec());
            _codecSetting = codecSetting ?? new CodecSetting();
            _memoryProvider = memoryProvider ?? ArrayPoolMemoryProvider<char>.Default;
        }

        public EncodingContext Context { get; }

        public void Dispose()
        {
        }

        public ReadOnlySpan<byte> Encode(ReadOnlyMemory<char> headerContent, Span<byte> buffer, bool isHttps = true)
        {
            var offset = 0;

            foreach (var headerField in Http11Parser.Read(headerContent, isHttps)) {
                offset += Encode(headerField, buffer.Slice(offset));
            }

            return buffer.Slice(0, offset);
        }

        private int Encode(in HeaderField entry, in Span<byte> buffer)
        {
            int index;

            if (Context.TryGetEntry(entry.Name, entry.Value, out index)) {
                // Existing 
                WritePrefix(buffer, 0x80, 1);
                var length = _primitiveOperation.WriteInt32(buffer, index, 7);

                return length;
            }

            if (Context.TryGetEntry(entry.Name, out index)) {
                // Header is present on the table with no value 

                if (_codecSetting.EncodedHeaders.Contains(entry.Name)) {
                    // Let's go save this entry on table 
                    Context.Register(entry.Name.Span, entry.Value.Span);

                    WritePrefix(buffer, 0x40, 2);
                    var length = _primitiveOperation.WriteInt32(buffer, index, 6);
                    var slicedBuff = buffer.Slice(length);

                    if (entry.Value.Length > slicedBuff.Length) {
                        throw new HPackCodecException(
                            "Length of string value " +
                            $"({entry.Value.Length}) exceed the maximum buffer {slicedBuff.Length}");
                    }

                    var res = InternalWriteString(entry.Value, slicedBuff);
                    length += res.Length;

                    return length;
                }
                else {
                    // Value is not meant to be saved 

                    WritePrefix(buffer, 0x0, 4);
                    var length = _primitiveOperation.WriteInt32(buffer, index, 4);
                    length += InternalWriteString(entry.Value, buffer.Slice(length)).Length;

                    return length;
                }
            }

            if (_codecSetting.EncodedHeaders.Contains(entry.Name)) {
                // Header are meant to be fully saved 
                //// Value of this header field should be saved on dynamic table

                Context.Register(entry.Name.Span, entry.Value.Span);

                var length = 1;
                buffer[0] = 0x40;

                Span<char> lowerCaseBuffer = stackalloc char[entry.Name.Length];
                entry.Name.Span.ToLowerInvariant(lowerCaseBuffer);

                length += InternalWriteString(lowerCaseBuffer, buffer.Slice(length)).Length;
                length += InternalWriteString(entry.Value, buffer.Slice(length)).Length;

                return length;
            }

            {
                var length = 1;
                buffer[0] = 0x0;

                char[]? heapBuffer = null;

                try {
                    var lowerCaseBuffer = entry.Name.Length < 1024
                        ? stackalloc char[entry.Name.Length]
                        : heapBuffer = ArrayPool<char>.Shared.Rent(entry.Name.Length);

                    entry.Name.Span.ToLowerInvariant(lowerCaseBuffer);

                    length += InternalWriteString(lowerCaseBuffer, buffer.Slice(length)).Length;
                    length += InternalWriteString(entry.Value, buffer.Slice(length)).Length;

                    return length;
                }
                finally {
                    if (heapBuffer != null)
                        ArrayPool<char>.Shared.Return(heapBuffer);
                }
            }
        }

        private Span<byte> InternalWriteString(ReadOnlySpan<char> input, Span<byte> buffer)
        {
            return _primitiveOperation.WriteString(input, buffer,
                _codecSetting.MaxLengthUncompressedString < input.Length);
        }

        private Span<byte> InternalWriteString(ReadOnlyMemory<char> input, Span<byte> buffer)
        {
            return _primitiveOperation.WriteString(input.Span, buffer,
                _codecSetting.MaxLengthUncompressedString < input.Length);
        }

        private void WritePrefix(Span<byte> buffer, byte value, int suffix)
        {
            buffer[0] = (byte) (buffer[0] & (0xFF >> suffix));
            buffer[0] = (byte) (buffer[0] | value);
        }

        public static HPackEncoder Create(CodecSetting codeSetting)
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;

            return new HPackEncoder(new EncodingContext(memoryProvider), codeSetting, memoryProvider);
        }
    }
}
