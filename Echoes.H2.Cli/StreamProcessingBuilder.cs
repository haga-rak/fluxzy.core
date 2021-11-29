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
        private readonly ArrayPool<byte> _arrayPool;
        private readonly HeaderEncoder _headerEncoder;

        public StreamProcessingBuilder(
            CancellationToken localCancellationToken, 
            UpStreamChannel upStreamChannel,
            H2StreamSetting streamSetting, 
            ArrayPool<byte> arrayPool)
        {
            _localCancellationToken = localCancellationToken;
            _upStreamChannel = upStreamChannel;
            _streamSetting = streamSetting;
            _arrayPool = arrayPool;

            var hPackEncoder = HPackEncoder.Create(new CodecSetting());

            _headerEncoder = new HeaderEncoder(hPackEncoder, _streamSetting);
        }

        public StreamProcessing Build(int streamIdentifier, StreamPool parent, CancellationToken callerCancellationToken)
        {
            return new StreamProcessing(streamIdentifier, parent, callerCancellationToken, _localCancellationToken,
                _upStreamChannel, _headerEncoder, new H2Message(), _streamSetting, _arrayPool);
        }
    }

    internal interface IStreamProcessingBuilder
    {
        StreamProcessing Build(int streamIdentifier, StreamPool parent, CancellationToken callerCancellationToken);
    }

}