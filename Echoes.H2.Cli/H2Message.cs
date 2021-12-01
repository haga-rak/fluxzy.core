using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding.HPack;

namespace Echoes.H2.Cli
{
    public class H2Message : IDisposable, IAsyncDisposable
    {
        private readonly HPackDecoder _hPackDecoder;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly StreamProcessing _owner;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private  StringBuilder _headerBuilder = new StringBuilder();
        private H2ResponseStream _lazyResponse; 

        internal H2Message(
            HPackDecoder hPackDecoder,
            int streamIdentifier,
            MemoryPool<byte> memoryPool, 
            StreamProcessing owner)
        {
            _hPackDecoder = hPackDecoder;
            _memoryPool = memoryPool;
            _owner = owner;
            StreamIdentifier = streamIdentifier;
        }

        internal int StreamIdentifier { get; set; }

        public string Header { get; private set; }

        public bool Complete { get; private set; } = false;

        public void OnDataConsumedByCaller(int dataSize)
        {
            _owner.OnDataConsumedByCaller(dataSize);
            ConsumedBodyLength += dataSize; 
        }

        public int ConsumedBodyLength { get; private set; } = 0; 

        public H2ResponseStream Response => _lazyResponse ??= new H2ResponseStream(_memoryPool, this);

        internal void PostResponseHeader(ReadOnlyMemory<byte> initialBytes, bool endHeader)
        {
            Span<char> buffer = stackalloc char[8192];
            _headerBuilder.Append(_hPackDecoder.Decode(initialBytes.Span, buffer));

            if (endHeader)
            {
                Header = _headerBuilder.ToString();
                _headerBuilder = null;
            }
        }

        internal void PostResponseBodyFragment(ReadOnlyMemory<byte> memory, bool end)
        {
            var response = Response; 
            response.Feed(memory, end);
            Complete = end; 
        }

        private byte[] _buffer = new byte[1024 * 16];

        public async Task<string> ResponseToString()
        {
            while (await Response.ReadAsync(_buffer, 0, _buffer.Length).ConfigureAwait(false) > 0)
            {

            }

            return string.Empty; 
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _lazyResponse.Dispose();
            _lazyResponse = null; 
        }


        public async ValueTask DisposeAsync()
        {
            if (_lazyResponse != null)
            {
                await _lazyResponse.DisposeAsync().ConfigureAwait(false);
                _lazyResponse = null; 
            }
        }
    }
}