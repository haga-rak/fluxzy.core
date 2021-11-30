using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.Encoding.HPack;

namespace Echoes.H2.Cli
{
    public class H2Message : IDisposable
    {
        private readonly HPackDecoder _hPackDecoder;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Channel<ReadOnlyMemory<byte>> _resultChannel = Channel.CreateBounded<ReadOnlyMemory<byte>>(new BoundedChannelOptions(1)
        {
            SingleWriter = true, 
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        private  StringBuilder _headerBuilder = new StringBuilder(); 

        internal H2Message(HPackDecoder hPackDecoder)
        {
            _hPackDecoder = hPackDecoder;
        }

        internal void PostRequestHeader(ReadOnlyMemory<byte> initialBytes, bool endHeader)
        {
            Span<char> buffer = stackalloc char [8192];
            _headerBuilder.Append(_hPackDecoder.Decode(initialBytes.Span, buffer));

            if (endHeader)
            {
                Header = _headerBuilder.ToString();
                _headerBuilder = null;
            }
        }

        public string Header { get; private set; }

        public bool Complete { get; private set; } = false;

        public IAsyncEnumerable<ReadOnlyMemory<byte>> Response => _resultChannel.Reader.ReadAllAsync(_cancellationTokenSource.Token);
        
        internal async Task PostRequestBodyFragment(ReadOnlyMemory<byte> memory, bool end,  CancellationToken cancellationToken)
        {
            await _resultChannel.Writer.WriteAsync(memory, cancellationToken).ConfigureAwait(false);

            if (end)
            {
                Complete = true; 
                _resultChannel.Writer.Complete();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        public async Task<string> ResponseToString()
        {
            StringBuilder builder = new StringBuilder();

            await foreach(var memory in Response)
            {
                builder.Append(System.Text.Encoding.UTF8.GetString(memory.Span));
            }

            return builder.ToString();
        }
    }
}