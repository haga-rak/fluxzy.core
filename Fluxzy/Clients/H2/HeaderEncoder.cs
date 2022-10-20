// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.H2
{
    public class HeaderEncoder : IHeaderEncoder
    {
        private readonly HPackEncoder _hPackEncoder;
        private readonly HPackDecoder _hPackDecoder;
        private readonly H2StreamSetting _streamSetting;

        public HeaderEncoder(
            HPackEncoder hPackEncoder,
            HPackDecoder hPackDecoder,
            H2StreamSetting streamSetting)
        {
            _hPackEncoder = hPackEncoder;
            _hPackDecoder = hPackDecoder;
            _streamSetting = streamSetting;
        }

        public HPackEncoder Encoder => _hPackEncoder;

        public HPackDecoder Decoder => _hPackDecoder;

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, RsBuffer destinationBuffer, bool endStream)
        {
            // heavy assumption that encoded length is at much twice larger than inital chars
            var encodedMaxLength = encodingJob.Data.Length * 2;
            byte[]? heapBuffer = null;

            try {

                Span<byte> buffer = encodedMaxLength < 1024 ? stackalloc byte[encodedMaxLength]
                        : heapBuffer = ArrayPool<byte>.Shared.Rent(encodedMaxLength);

                var encodedHeader = _hPackEncoder.Encode(encodingJob.Data, buffer);

                var res = Packetizer.PacketizeHeader(
                    encodedHeader, destinationBuffer.Buffer,
                    endStream, encodingJob.StreamIdentifier,
                    _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

                return destinationBuffer.Memory.Slice(0, res.Length);
            }
            finally {
                if (heapBuffer != null) {
                    ArrayPool<byte>.Shared.Return(heapBuffer);
                }
            }
        }

        public ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            var res = _hPackDecoder.Decode(encodedBuffer.Span, destinationBuffer.Span);
            return destinationBuffer.Slice(0, res.Length);
        }
    }
}