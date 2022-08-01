// Copyright © 2021 Haga Rakotoharivelo

using System;
using Fluxzy.Clients.H2.Encoder;

namespace Fluxzy.Clients.H2
{
    public class HeaderEncoder : IHeaderEncoder
    {
        private readonly HPackEncoder _hpackEncoder;
        private readonly HPackDecoder _hpackDecoder;
        private readonly H2StreamSetting _streamSetting;

        public HeaderEncoder(
            HPackEncoder hpackEncoder,
            HPackDecoder hpackDecoder,
            H2StreamSetting streamSetting)
        {
            _hpackEncoder = hpackEncoder;
            _hpackDecoder = hpackDecoder;
            _streamSetting = streamSetting;
        }

        public HPackEncoder Encoder => _hpackEncoder;

        public HPackDecoder Decoder => _hpackDecoder;

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer, bool endStream)
        {
            Span<byte> buffer = stackalloc byte[_streamSetting.Remote.MaxHeaderLine];
            var encodedHeader = _hpackEncoder.Encode(encodingJob.Data, buffer);

            var res = Packetizer.PacketizeHeader(
                encodedHeader, destinationBuffer.Span, 
                endStream, encodingJob.StreamIdentifier,
                _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

            return destinationBuffer.Slice(0, res.Length);
        }

        public ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            var res = _hpackDecoder.Decode(encodedBuffer.Span, destinationBuffer.Span);
            return destinationBuffer.Slice(0, res.Length);
        }
    }
}