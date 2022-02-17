using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Encoder;

namespace Echoes.H2
{
    public sealed class H2Message : IDisposable, IAsyncDisposable
    {
        private readonly HPackDecoder _hPackDecoder;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly StreamProcessing _owner;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private IList<HeaderField> _headerFields = new List<HeaderField>(); 

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

        public H2ResponseStream ResponseStream => _lazyResponse ??= new H2ResponseStream(_memoryPool, this);

        public IReadOnlyCollection<HeaderField> HeaderFields => new ReadOnlyCollection<HeaderField>(_headerFields);

        internal void PostResponseHeader(ReadOnlyMemory<byte> initialBytes, bool endHeader)
        {
            Span<char> buffer = stackalloc char[16384];
            _headerBuilder.Append(_hPackDecoder.Decode(initialBytes.Span, buffer, ref _headerFields));

            if (endHeader)
            {
                Header = _headerBuilder.ToString();
                _headerBuilder = null;
            }
        }

        internal void PostResponseBodyFragment(ReadOnlyMemory<byte> memory, bool end)
        {
            var response = ResponseStream; 
            response.Feed(memory, end);
            Complete = end; 
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