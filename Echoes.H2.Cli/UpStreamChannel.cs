using System;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public delegate ValueTask UpStreamChannel(WriteTask data, CancellationToken token);
    
}