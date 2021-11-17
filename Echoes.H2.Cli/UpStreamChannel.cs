using System;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public delegate ValueTask UpStreamChannel(Memory<byte> data, CancellationToken token);
    
}