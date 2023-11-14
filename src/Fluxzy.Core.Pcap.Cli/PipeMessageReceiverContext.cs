// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Cli
{
    public class PipeMessageReceiverContext : IAsyncDisposable
    {
        private readonly ICaptureContext _internalCapture;

        public PipeMessageReceiverContext(ICaptureContext captureContext, CancellationToken token)
        {
            _internalCapture = captureContext;

            Receiver = new PipeMessageReceiver(
                message => _internalCapture.Subscribe(message.OutFileName, message.RemoteAddress, message.RemotePort,
                    message.LocalPort),
                message => _internalCapture.StoreKey(message.NssKey, message.RemoteAddress, message.RemotePort,
                    message.LocalPort),
                unsubscribeMessage => _internalCapture.Unsubscribe(unsubscribeMessage.Key),
                includeMessage => _internalCapture.Include(includeMessage.RemoteAddress, includeMessage.RemotePort),
                () => _internalCapture.Flush(),
                () => _internalCapture.ClearAll(),
                token
            );
        }

        public PipeMessageReceiver? Receiver { get; }

        public async ValueTask DisposeAsync()
        {
            await _internalCapture.DisposeAsync();
        }

        public void Start()
        {
            _internalCapture.Start();
        }

        public async Task<int> WaitForExit()
        {
            return await Receiver!.WaitForExit();
        }
    }
}
