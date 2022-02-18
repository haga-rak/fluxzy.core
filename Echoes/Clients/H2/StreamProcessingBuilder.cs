// Copyright © 2021 Haga Rakotoharivelo

using System.Buffers;
using System.Threading;
using Echoes.H2.Encoder;
using Echoes.H2.Encoder.Utils;

namespace Echoes.H2
{
    internal class StreamProcessingBuilder : IStreamProcessingBuilder
    {
        private readonly CancellationToken _localCancellationToken;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly H2StreamSetting _streamSetting;
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly Http11Parser _parser;
        private readonly HeaderEncoder _headerEncoder;
        private readonly HPackDecoder _hPackDecoder;

        public StreamProcessingBuilder(
            Authority authority,
            CancellationToken localCancellationToken, 
            UpStreamChannel upStreamChannel,
            H2StreamSetting streamSetting,
            WindowSizeHolder overallWindowSizeHolder,
            ArrayPool<byte> arrayPool,
            Http11Parser parser)
        {
            _localCancellationToken = localCancellationToken;
            _upStreamChannel = upStreamChannel;
            _streamSetting = streamSetting;
            _overallWindowSizeHolder = overallWindowSizeHolder;
            _arrayPool = arrayPool;
            _parser = parser;

            var codecSetting = new CodecSetting();

            _hPackDecoder = HPackDecoder.Create(codecSetting, authority);
            var hPackEncoder = HPackEncoder.Create(codecSetting);
            _headerEncoder = new HeaderEncoder(hPackEncoder, _hPackDecoder, _streamSetting);
        }

        public StreamProcessing Build(
            int streamIdentifier, StreamPool parent,
            Exchange exchange, CancellationToken callerCancellationToken)
        {
            return new StreamProcessing(streamIdentifier, parent, exchange,
                _upStreamChannel, _headerEncoder, _streamSetting,
                _overallWindowSizeHolder, _parser, _localCancellationToken, callerCancellationToken);
        }
    }

    internal interface IStreamProcessingBuilder
    {
        StreamProcessing Build(int streamIdentifier, StreamPool parent, Exchange exchange, CancellationToken callerCancellationToken);
    }

}