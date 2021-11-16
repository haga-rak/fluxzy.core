using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Cli.IO;

namespace Echoes.H2.Cli
{
    internal interface IUpStreamConnection
    {
        Task Write(byte[] buffer, int offset, int length, CancellationToken cancellationToken);
    }

    internal class ActiveStream
    {
        private readonly byte [] _windowBuffer; 
        private readonly byte [] _buffer = new byte[1024 * 16]; 

        private readonly ChannelWriter<Stream> _upStreamWriter;
        private readonly IUpStreamConnection _upStreamConnection;
        private readonly int _maxPacketSize;

        private readonly Channel<Stream> _downStreamChannel;

        public ActiveStream(
            int streamIdentifier,
            ChannelWriter<Stream> upStreamWriter,
            IUpStreamConnection upStreamConnection,
            int localWindowSize, int maxPacketSize)
        {
            StreamIdentifier = streamIdentifier;
            _upStreamWriter = upStreamWriter;
            _upStreamConnection = upStreamConnection;
            _maxPacketSize = maxPacketSize;
            LocalWindowSize = localWindowSize;
            _windowBuffer = new byte[localWindowSize];
            Used = true;

            _downStreamChannel = Channel.CreateUnbounded<Stream>(new UnboundedChannelOptions()
            {
                SingleReader = true, 
                SingleWriter = true,
            }); 
        }

        public int StreamIdentifier { get;  }

        public bool Used { get; internal set; }

        public StreamStateType Type { get; internal set; } = StreamStateType.Idle;

        public int LocalWindowSize { get; set; }

        public int RemoteWindowSize { get; set; }

        public ValueTask WriteHeader(Stream t, CancellationToken cancellationToken)
        {
            return _upStreamWriter.WriteAsync(new H2HeaderEncodeStream(t), cancellationToken);
        }

        /// <summary>
        /// Lit l'entête à partir de la connexion 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task ReadHeaderFromConnection(Stream stream)
        {

        }

        public ValueTask WriteBody(Stream t, CancellationToken cancellationToken)
        {
            return _upStreamWriter.WriteAsync(t, cancellationToken);
        }

        public async ValueTask ReadBody(Stream t, CancellationToken cancellationToken)
        {
            var bodyStream = await _downStreamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            await bodyStream.CopyToAsync(t, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask ReadHeader(Stream t, CancellationToken cancellationToken)
        {
            var bodyStream = await _downStreamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            await bodyStream.CopyToAsync(new H2HeaderDecodeStream(t), cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask Receive(Stream stream, CancellationToken cancellationToken)
        {
            await _downStreamChannel.Writer.WriteAsync(stream, cancellationToken).ConfigureAwait(false); 
        }
        
    }
    
}