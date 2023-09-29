// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.H2
{
    public class HeaderEncoder : IHeaderEncoder
    {
        private readonly H2StreamSetting _streamSetting;

        public HeaderEncoder(
            HPackEncoder hPackEncoder,
            HPackDecoder hPackDecoder,
            H2StreamSetting streamSetting)
        {
            Encoder = hPackEncoder;
            Decoder = hPackDecoder;
            _streamSetting = streamSetting;
        }

        public HPackEncoder Encoder { get; }

        public HPackDecoder Decoder { get; }

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, RsBuffer destinationBuffer, bool endStream)
        {
            // heavy assumption that encoded length is at much twice larger than initial chars
            var encodedMaxLength = encodingJob.Data.Length * 2;
            byte[]? heapBuffer = null;

            try {
                var buffer = encodedMaxLength < 1024
                    ? stackalloc byte[encodedMaxLength]
                    : heapBuffer = ArrayPool<byte>.Shared.Rent(encodedMaxLength);

                var encodedHeader = Encoder.Encode(encodingJob.Data, buffer);

                var res = Packetizer.PacketizeHeader(
                    encodedHeader, destinationBuffer.Buffer,
                    endStream, encodingJob.StreamIdentifier,
                    _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

                return destinationBuffer.Memory.Slice(0, res.Length);
            }
            finally {
                if (heapBuffer != null)
                    ArrayPool<byte>.Shared.Return(heapBuffer);
            }
        }

        public ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            var res = Decoder.Decode(encodedBuffer.Span, destinationBuffer.Span);

            return destinationBuffer.Slice(0, res.Length);
        }
    }
}
