namespace Fluxzy.Interop.Pcap.Cli;

public class PipeMessageReceiverContext : IAsyncDisposable
{
    private readonly CancellationToken _token;
    private readonly ICaptureContext _internalCapture;

    public PipeMessageReceiverContext(ICaptureContext captureContext, CancellationToken token)
    {
        _token = token;
        _internalCapture = captureContext;
    }

    public async Task LoopReceiver()
    {
        var receiver = new PipeMessageReceiver(
            (message) => _internalCapture.Subscribe(message.OutFileName, message.RemoteAddress, message.RemotePort,
                message.LocalPort),
            unsubscribeMessage => _internalCapture.Unsubscribe(unsubscribeMessage.Key),
            (includeMessage) => _internalCapture.Include(includeMessage.RemoteAddress, includeMessage.RemotePort),
            _token
        );

        await receiver.WaitForExit(); 
    }

    public async ValueTask DisposeAsync()
    {
        await _internalCapture.DisposeAsync();
    }
}