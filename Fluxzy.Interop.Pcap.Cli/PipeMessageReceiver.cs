using System.IO.Pipes;
using Fluxzy.Capturing.Messages;

namespace Fluxzy.Interop.Pcap.Cli;

public class PipeMessageReceiver
{
    private readonly Func<SubscribeMessage, long> _subscribeHandler;
    private readonly Action<UnsubscribeMessage> _unsubscribeHandler;
    private readonly Action<IncludeMessage> _includeHandler;
    private readonly CancellationToken _token;
    private readonly NamedPipeServerStream _pipeServer;
    private readonly Task _taskLoop;
    
    public PipeMessageReceiver(string pipeName, 
        Func<SubscribeMessage, long> subscribeHandler,
        Action<UnsubscribeMessage> unsubscribeHandler,
        Action<IncludeMessage> includeHandler,
        CancellationToken token)
    {
        _subscribeHandler = subscribeHandler;
        _unsubscribeHandler = unsubscribeHandler;
        _includeHandler = includeHandler;
        _token = token;
        _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
        _taskLoop = InternalLoop(); 
    }
    
    private async Task InternalLoop()
    {
        await _pipeServer.WaitForConnectionAsync(_token);

        var binaryWriter = new BinaryWriter(_pipeServer);
        var binaryReader = new BinaryReader(_pipeServer);
        var @byte = new byte[1];

        while ( (await _pipeServer.ReadAsync(@byte, 0, 1, _token))  > 0) {
            var messageType = (MessageType) @byte[0]; 
        
            switch (messageType) {
                case MessageType.Subscribe:
                    var subscribeMessage = SubscribeMessage.FromReader(binaryReader);
                    var key = _subscribeHandler(subscribeMessage);
                    binaryWriter.Write(key);
                    break;
                case MessageType.Unsubscribe:
                    var unsubscribeMessage = UnsubscribeMessage.FromReader(binaryReader);
                    _unsubscribeHandler(unsubscribeMessage);
                    break;
                case MessageType.Include:
                    var includeMessage = IncludeMessage.FromReader(binaryReader);
                    _includeHandler(includeMessage);
                    break;
                case MessageType.Exit:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        } 
    }

    public async ValueTask WaitForExit()
    {
        await _taskLoop; 
    }
}