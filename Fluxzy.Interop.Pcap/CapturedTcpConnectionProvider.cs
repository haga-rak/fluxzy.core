// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Core;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly CaptureContext _captureContext;

        public CapturedTcpConnectionProvider()
        {
            _captureContext = new CaptureContext();
        }

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(_captureContext, dumpFileName);
        }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await _captureContext.DisposeAsync();
        }
    }
}