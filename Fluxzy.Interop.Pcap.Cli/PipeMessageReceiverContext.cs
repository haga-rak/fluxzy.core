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
            unsubscribeMessage => _internalCapture.Unsubscribe(unsubscribeMessage.Key),
            (includeMessage) => _internalCapture.Include(includeMessage.RemoteAddress, includeMessage.RemotePort),
            ( ) => _internalCapture.Flush(),
            _token
        );
    }

    public void Start()
    {
        _internalCapture.Start();
    }
    
    public Task WaitForExit()
    {
        return Receiver!.WaitForExit().AsTask();
    }
    
    

    public async ValueTask DisposeAsync()
    {
        await _internalCapture.DisposeAsync();
    }
}