// Copyright © 2021 Haga Rakotoharivelo

using System;
using Fluxzy.Clients.H2.Encoder;

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

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer, bool endStream)
        {
            Span<byte> buffer = stackalloc byte[_streamSetting.Remote.MaxHeaderLine];
            var encodedHeader = _hPackEncoder.Encode(encodingJob.Data, buffer);

            var res = Packetizer.PacketizeHeader(
                encodedHeader, destinationBuffer.Span, 
                endStream, encodingJob.StreamIdentifier,
                _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

            return destinationBuffer.Slice(0, res.Length);
        }

        public ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            var res = _hPackDecoder.Decode(encodedBuffer.Span, destinationBuffer.Span);
            return destinationBuffer.Slice(0, res.Length);
        }
    }
}