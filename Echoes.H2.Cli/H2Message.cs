using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public class H2Message : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Channel<Memory<byte>> _resultChannel = Channel.CreateBounded<Memory<byte>>(new BoundedChannelOptions(1)
        {
            SingleWriter = true, 
            SingleReader = true,
            AllowSynchronousContinuations = true
        }); 

        internal H2Message()
        {

        }

        public Memory<byte> Header { get; private set; }

        public bool Complete { get; private set; } = false;

        public IAsyncEnumerable<Memory<byte>> Response => _resultChannel.Reader.ReadAllAsync(_cancellationTokenSource.Token);
        
        internal void PostRequestHeader(Memory<byte> data)
        {
            Header = data; 
        }

        internal async Task PostRequestBodyFragment(Memory<byte> memory, bool end,  CancellationToken cancellationToken)
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
    }
}