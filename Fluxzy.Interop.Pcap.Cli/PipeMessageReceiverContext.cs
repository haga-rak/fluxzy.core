namespace Fluxzy.Interop.Pcap.Cli;

public class PipeMessageReceiverContext : IAsyncDisposable
{
    private readonly CancellationToken _token;
    private readonly ICaptureContext _internalCapture;
    public PipeMessageReceiver? Receiver { get; private set; }

    public PipeMessageReceiverContext(ICaptureContext captureContext, CancellationToken token)
    {
        _token = token;
        _internalCapture = captureContext;
        
        Receiver = new PipeMessageReceiver(
            (message) => _internalCapture.Subscribe(message.OutFileName, message.RemoteAddress, message.RemotePort,
                message.LocalPort),
            (message) => _internalCapture.StoreKey(message.NssKey, message.RemoteAddress, message.RemotePort,
                message.LocalPort),
            unsubscribeMessage => _internalCapture.Unsubscribe(unsubscribeMessage.Key),
            (includeMessage) => _internalCapture.Include(includeMessage.RemoteAddress, includeMessage.RemotePort),
            ( ) => _internalCapture.Flush(),
            ( ) => _internalCapture.ClearAll(),
            _token
        );
    }

    public void Start()
    {
        _internalCapture.Start();
    }
    
    public async Task<int> WaitForExit()
    {
        return await Receiver!.WaitForExit();
    }

    public async ValueTask DisposeAsync()
    {
        await _internalCapture.DisposeAsync();
    }
}