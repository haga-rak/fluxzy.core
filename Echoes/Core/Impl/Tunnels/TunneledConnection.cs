using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    //internal class TunneledConnection : IDisposable
    //{
    //    private readonly IDownStreamConnection _down;
    //    private readonly IUpstreamConnection _up;
    //    private readonly IReferenceClock _referenceClock;
    //    private readonly Func<HttpExchange, Task> _exchangeListener;
    //    private readonly Task _upWrite;
    //    private readonly Task _downWrite;
    //    private readonly CancellationTokenSource _haltToken = new CancellationTokenSource();

    //    private TunnelMessage _current; 

    //    public TunneledConnection(
    //        IDownStreamConnection down, 
    //        IUpstreamConnection up, 
    //        Stream bandwidthLimiterStream, 
    //        IReferenceClock referenceClock,
    //        Func<HttpExchange, Task> exchangeListener)
    //    {
    //        _down = down;
    //        _up = up;
    //        _referenceClock = referenceClock;
    //        _exchangeListener = exchangeListener;
    //        _upWrite = StreamCopy(down, up, null, OnRequestReceived, _haltToken.Token); // We do not limit upload
    //        _downWrite = StreamCopy(up, down, bandwidthLimiterStream, OnResponseReceived, _haltToken.Token);

    //        if (_exchangeListener != null)
    //            OnDone(_upWrite, _downWrite);
    //    }

    //    private void OnDone(Task down, Task up)
    //    {
    //        Task.WhenAll(down, up).ContinueWith(async t =>
    //        {
    //            if (_current != null)
    //            {
    //                // We send the last exchange when connection close
    //                await _exchangeListener(_current.ProduceExchange(_down, _up)).ConfigureAwait(false);
    //            }
    //        }); 
    //    }

    //    private async Task OnRequestReceived(long size)
    //    {
    //        if (_current == null)
    //            _current = new TunnelMessage(_referenceClock);

    //        if (_current.UpStreamSent > 0)
    //        {
    //            var oldCurrent = _current;
    //            _current = null;

    //            if (_exchangeListener != null)
    //                await _exchangeListener(oldCurrent.ProduceExchange(_down, _up)).ConfigureAwait(false);

    //            _current = new TunnelMessage(_referenceClock);
    //        }

    //        _current.AddDownStreamData(size);
    //    }

    //    private Task OnResponseReceived(long size)
    //    {
            
    //        _current?.AddUpStreamData(size);

    //        return Task.CompletedTask;
    //    }

    //    private static async Task StreamCopy(
    //        IConnection inConnection, 
    //        IConnection outConnection, 
    //        Stream bandwidthLimiterStream, 
    //        Func<long, Task> onByteReceived, CancellationToken token)
    //    {
            
    //        byte [] buffer = new byte [1024 * 32];

    //        try
    //        {
    //            // No need to cancel NetworkStream as it's not cancellable 

    //            int readen;
    //            while ((readen = await inConnection.ReadStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
    //            {
    //                await onByteReceived(readen).ConfigureAwait(false);

    //                var writeTasks = new List<Task> { outConnection.WriteStream.WriteAsync(buffer, 0, readen, token) };

    //                if (token.IsCancellationRequested)
    //                {
    //                    break; 
    //                }

    //                if (bandwidthLimiterStream != null)
    //                {
    //                    writeTasks.Add(bandwidthLimiterStream.WriteAsync(buffer, 0, readen, token));
    //                }

    //                await Task.WhenAll(writeTasks).ConfigureAwait(false);
    //            }
    //        }
    //        catch (Exception) 
    //        {
    //            // Close both connection when done 

    //            try
    //            {
    //                inConnection.Dispose();
    //            }
    //            catch
    //            {
    //                // Ignored
    //            }

    //            try
    //            {
    //                outConnection.Dispose();
    //            }
    //            catch
    //            {
    //                // Ignored
    //            }
    //        }
    //    }
        
    //    public void Dispose()
    //    {

    //       _haltToken.Cancel();

    //        try
    //        {
    //            _down.Dispose();
    //        }
    //        catch 
    //        {

    //        }

    //        try
    //        {
    //            _up.Dispose();
    //        }
    //        catch 
    //        {

    //        }

    //        _haltToken.Dispose();

    //        _upWrite.ConfigureAwait(false).GetAwaiter().GetResult(); 
    //        _downWrite.ConfigureAwait(false).GetAwaiter().GetResult();

    //    }
    //}

    //internal class TunnelMessage
    //{
    //    private long _downStreamSent = -1;
    //    private long _upStreamSent = -1;

    //    private readonly IReferenceClock _referenceClock;

    //    public TunnelMessage(IReferenceClock referenceClock)
    //    {
    //        _referenceClock = referenceClock;
    //    }

    //    public long DownStreamSent => _downStreamSent;

    //    public long UpStreamSent => _upStreamSent;


    //    public DateTime? DownStreamStartSendingHeader { get; internal set; }

    //    public DateTime? BodySentToUpStream { get; internal set; }

    //    public DateTime? UpStreamStartSendingHeader { get; set; }

    //    public DateTime? UpStreamCompleteSendingBody { get; set; }

    //    public void AddDownStreamData(long size)
    //    {
    //        if (_downStreamSent < 0)
    //        {
    //            DownStreamStartSendingHeader = _referenceClock.Instant();
    //            _downStreamSent = 0;
    //        }

    //        BodySentToUpStream = _referenceClock.Instant();
    //        Interlocked.Add(ref _downStreamSent, size);
    //    }

    //    public void AddUpStreamData(long size)
    //    {
    //        if (_upStreamSent < 0)
    //        {
    //            UpStreamStartSendingHeader = _referenceClock.Instant();
    //            _upStreamSent = 0; 
    //        }

    //        UpStreamCompleteSendingBody = _referenceClock.Instant();

    //        Interlocked.Add(ref _upStreamSent, size);
    //    }

    //    public HttpExchange ProduceExchange(IDownStreamConnection down, IUpstreamConnection up)
    //    {
    //        var requestMessage = new Hrm()
    //        {
    //            BodySentToUpStream = BodySentToUpStream,
    //            DownStreamStartSendingHeader = DownStreamStartSendingHeader,
    //            OnWireContentLength = DownStreamSent,
    //            DestinationHost = $"{down.TargetHostName}:{down.TargetPort}",
    //            Body = Encoding.ASCII.GetBytes($"Encrypted data of size {DownStreamSent}"),
    //            Encrypted = true
    //        };

    //        var responseMessage = new Hpm(requestMessage.Id)
    //        {
    //            UpStreamStartSendingHeader = UpStreamStartSendingHeader,
    //            UpStreamCompleteSendingBody = UpStreamCompleteSendingBody,
    //            OnWireContentLength = UpStreamSent,
    //            Body = Encoding.ASCII.GetBytes($"Encrypted data of size {UpStreamSent}"),
    //            Encrypted = true
    //        };

    //        return new HttpExchange(requestMessage, responseMessage, down, up);
    //    }
    //}
}
