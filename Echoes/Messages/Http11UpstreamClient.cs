using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Echoes.Core;
using Echoes.Core.Utils;

namespace Echoes
{
    //internal class Http11UpstreamClient : IUpstreamClient
    //{
    //    private readonly Destination _destination;
    //    private readonly IServerChannelPoolManager _poolManager;
    //    private readonly IReferenceClock _referenceClock;
    //    private IUpstreamConnection _upStreamConnection;

    //    public Http11UpstreamClient(Destination destination, IServerChannelPoolManager poolManager, IReferenceClock referenceClock)
    //    {
    //        _destination = destination;
    //        _poolManager = poolManager;
    //        _referenceClock = referenceClock;

    //        if (_destination.DestinationType != DestinationType.Secure &&
    //            _destination.DestinationType != DestinationType.Insecure)
    //        {
    //            throw new InvalidOperationException("Unhandled destination type"); ;
    //        }
    //    }


    //    public async Task Init()
    //    {
    //        Stopwatch watch = new Stopwatch();

    //        watch.Start();

    //        _upStreamConnection = await
    //            _poolManager
    //                .GetRemoteConnection(_destination.Host, _destination.Port, _destination.DestinationType == DestinationType.Secure)
    //                .ConfigureAwait(false); // Negotiate HTTP2.


    //        watch.Stop();

    //        //Console.WriteLine($"waiting {watch.ElapsedMilliseconds}");

    //        EndPointInformation = new EndPointInformation(_upStreamConnection);
    //    }

    //    public EndPointInformation EndPointInformation { get; private set; }

    //    public async Task<Hpm> ProduceResponse(Hrm requestMessage, params Stream[] outStreams)
    //    {
    //        requestMessage.SendingHeaderToUpStream = _referenceClock.Instant();

    //        Stopwatch watch = new Stopwatch();

    //        //watch.Start();
    //        //    _upStreamConnection.WriteStream.Write(requestMessage.ForwardableHeader, 0, requestMessage.ForwardableHeader.Length);   
    //        await _upStreamConnection.WriteStream.WriteAsync(requestMessage.ForwardableHeader, 0, requestMessage.ForwardableHeader.Length).ConfigureAwait(false);

    //        requestMessage.HeaderSentToUpStream = _referenceClock.Instant();

    //        if (requestMessage.Body != null)
    //        {
    //            await _upStreamConnection.WriteStream.WriteAsync(requestMessage.Body, 0, requestMessage.Body.Length).ConfigureAwait(false);
    //        }

    //        watch.Stop();

    //       //  Console.WriteLine($"{watch.ElapsedMilliseconds}ms");

    //        requestMessage.BodySentToUpStream = _referenceClock.Instant();

    //        var crLfStreamReader = _upStreamConnection.GetHttpStreamReader();

    //        HeaderReadResult headerResult = null;
    //        Hpm httpPayload = null;
            
    //        headerResult = await crLfStreamReader.ReadHeaderAsync(false).ConfigureAwait(false);

    //        if (headerResult?.Buffer == null)
    //            return null;

    //        // L'entête est terminée 
    //        httpPayload = HttpMessageHeaderParser.BuildResponseMessage(requestMessage.Id, headerResult.Buffer);

    //        httpPayload.ServerConnected = _upStreamConnection.InstantConnected;
    //        httpPayload.UpStreamStartSendingHeader = headerResult.FirstByteReceived;
    //        httpPayload.UpStreamCompleteSendingHeader = headerResult.LastByteReceived;

    //        if (!httpPayload.Valid)
    //        {
    //            return null;
    //        }

    //        // Writing headers
    //        var headerBytes = httpPayload.ForwardableHeader;

    //        try
    //        {
    //            await outStreams.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
    //            // await WriteStream.FlushAsync().ConfigureAwait(false);

    //            httpPayload.UpStreamStartSendingBody = _referenceClock.Instant();

    //            if (!httpPayload.NoBody && ((httpPayload.ContentLength != 0)))
    //            {
    //                using (var bodyStream = new MemoryStream())
    //                {
    //                    var receiverStreams = new List<Stream>(outStreams) { bodyStream }.ToArray();

    //                    if (httpPayload.ContentLength > 0)
    //                    {
    //                        var bodyResult = await crLfStreamReader
    //                            .ReadBodyAsync(httpPayload.ContentLength, receiverStreams)
    //                            .ConfigureAwait(false);

    //                        httpPayload.UpStreamStartSendingBody = bodyResult.FirstByteReceived;
    //                        httpPayload.UpStreamCompleteSendingBody = bodyResult.LastByteReceived;

    //                        httpPayload.OnWireContentLength = bodyResult.Length;
    //                    }
    //                    else if (httpPayload.IsChunkedTransfert)
    //                    {
    //                        var bodyResult = await crLfStreamReader
    //                            .ReadBodyChunkedAsync(receiverStreams)
    //                            .ConfigureAwait(false);

    //                        httpPayload.UpStreamStartSendingBody = bodyResult.FirstByteReceived;
    //                        httpPayload.UpStreamCompleteSendingBody = bodyResult.LastByteReceived;

    //                        httpPayload.OnWireContentLength = bodyResult.Length;
    //                    }
    //                    else
    //                    {
    //                        var bodyResult = await crLfStreamReader
    //                            .ReadBodyUntilEofAsync(receiverStreams).ConfigureAwait(false);

    //                        httpPayload.UpStreamStartSendingBody = bodyResult.FirstByteReceived;
    //                        httpPayload.UpStreamCompleteSendingBody = bodyResult.LastByteReceived;

    //                        httpPayload.OnWireContentLength = bodyResult.Length;

    //                        // Abandon the connection after reading if server didn't send Content-Length or Chunked bod
    //                        httpPayload.ShouldCloseConnection = true;
    //                    }

    //                    httpPayload.Body = bodyStream.ToArray();

    //                    if (httpPayload.ContentLength > 0 && httpPayload.ContentLength != httpPayload.Body.Length)
    //                    {
    //                        httpPayload.AddError("Actual content length sent by the server is different than specified by Content-length header", HttpProxyErrorType.NetworkError, "No data");
    //                    }
    //                }
    //            }

    //            if (httpPayload.UpStreamStartSendingBody != null)
    //            {
    //                var now = _referenceClock.Instant();
    //                httpPayload.UpStreamStartSendingBody = now;
    //                httpPayload.UpStreamCompleteSendingBody = now;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            if (ex is EchoesException eex)
    //            {
    //                httpPayload.AddError(eex.Message, HttpProxyErrorType.NetworkError, eex.ToString());
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }

    //        return httpPayload;
    //    }

    //    public Task Release(bool shouldClose)
    //    {
    //        if (_upStreamConnection != null)
    //            return _poolManager.Return(_upStreamConnection, shouldClose);

    //        return Task.CompletedTask;
    //    }

    //    public IUpstreamConnection Detach()
    //    {
    //        var result = _upStreamConnection;

    //        _upStreamConnection = null;

    //        return result;
    //    }
    //}

    public static class StreamUtilities
    {
        public static async Task WriteAsync2(this Stream stream, byte[] buffer, int offset, int length)
        {
            if (length == 1)
            {
                 await stream.WriteAsync(buffer, offset, length).ConfigureAwait(false);
            }

            int division = length / 2;

            await stream.WriteAsync(buffer, 0, division).ConfigureAwait(false);
            await stream.WriteAsync(buffer, division, length - division).ConfigureAwait(false);
        }
    }
}