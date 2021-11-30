// Copyright © 2021 Haga Rakotoharivelo

using System.Buffers;
using System.Threading;
using Echoes.Encoding;
using Echoes.Encoding.HPack;

namespace Echoes.H2.Cli
{
    internal class StreamProcessingBuilder : IStreamProcessingBuilder
    {
        private readonly CancellationToken _localCancellationToken;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly H2StreamSetting _streamSetting;
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly HeaderEncoder _headerEncoder;
        private readonly HPackDecoder _hPackDecoder;

        public StreamProcessingBuilder(
            CancellationToken localCancellationToken, 
            UpStreamChannel upStreamChannel,
            H2StreamSetting streamSetting,
            WindowSizeHolder overallWindowSizeHolder,
            ArrayPool<byte> arrayPool)
        {
            _localCancellationToken = localCancellationToken;
            _upStreamChannel = upStreamChannel;
            _streamSetting = streamSetting;
            _overallWindowSizeHolder = overallWindowSizeHolder;
            _arrayPool = arrayPool;

            var codecSetting = new CodecSetting();

            _hPackDecoder = HPackDecoder.Create(codecSetting);

            var hPackEncoder = HPackEncoder.Create(codecSetting);
            

            _headerEncoder = new HeaderEncoder(hPackEncoder, _hPackDecoder, _streamSetting);
        }

        public StreamProcessing Build(int streamIdentifier, StreamPool parent, CancellationToken callerCancellationToken)
        {
            return new StreamProcessing(streamIdentifier, parent, callerCancellationToken, _localCancellationToken,
                _upStreamChannel, _headerEncoder, new H2Message(_hPackDecoder), _streamSetting, _overallWindowSizeHolder, _arrayPool);
        }
    }

    internal interface IStreamProcessingBuilder
    {
        StreamProcessing Build(int streamIdentifier, StreamPool parent, CancellationToken callerCancellationToken);
    }

}