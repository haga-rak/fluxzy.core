using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    //internal class ProxyMessageReader
    //{
    //    private readonly ProxyStartupSetting _startupSetting;
    //    private readonly ISecureConnectionUpdater _secureConnectionUpdater;
    //    private readonly IServerChannelPoolManager _channelPoolManager;
    //    private readonly IReferenceClock _referenceClock;
    //    private readonly ExchangeBuilder _exchangeBuilder;
    //    private readonly PoolBuilder _poolBuilder;
    //    private readonly ClientSetting _clientSetting;

    //    public ProxyMessageReader
    //    (
    //        ProxyStartupSetting startupSetting,
    //        ISecureConnectionUpdater secureConnectionUpdater,
    //        IServerChannelPoolManager channelPoolManager,
    //        IReferenceClock referenceClock,
    //        ExchangeBuilder exchangeBuilder,
    //        PoolBuilder poolBuilder, 
    //        ClientSetting clientSetting)
    //    {
    //        _startupSetting = startupSetting;
    //        _secureConnectionUpdater = secureConnectionUpdater;
    //        _channelPoolManager = channelPoolManager;
    //        _referenceClock = referenceClock;
    //        _exchangeBuilder = exchangeBuilder;
    //        _poolBuilder = poolBuilder;
    //        _clientSetting = clientSetting;
    //    }

    //    public async Task<Exchange> ReadNextMessage(TcpClient client, CancellationToken token)
    //    {
    //        var exchange = await _exchangeBuilder.BuildFromProxyRequest(
    //            client, _startupSetting, token);

    //        if (exchange == null)
    //            return null;

    //        var connection = await _poolBuilder.GetPool(exchange, _clientSetting, token); 



    //        try
    //        {
    //            while (true)
    //            {
    //                clientStreamReader = downStreamConnection.GetHttpStreamReader();

    //                downStreamConnection.UpgradeReadStream();

    //                // GETTING CLIENT REQUEST
    //                headerResult = 
    //                    await clientStreamReader.ReadHeaderAsync(true).ConfigureAwait(false);

    //                if (headerResult?.Buffer == null)
    //                    return new ProxyMessage(false); // EOF or connection closed

    //                // Building and analyzing header
    //                httpPayload = HttpMessageHeaderParser.BuildRequestMessage(
    //                    headerResult.Buffer,
    //                    downStreamConnection.TargetHostName,
    //                    downStreamConnection.TargetPort);

    //                if (!httpPayload.Valid)
    //                {
    //                    // Clients abort connection
    //                    return new ProxyMessage(false);
    //                }


    //                if (httpPayload?.ClientConnected == null)
    //                {
    //                    httpPayload.ClientConnected = downStreamConnection.InstantConnected;
    //                }

    //                // UPGRADE Current Connection to HTTPS or WebSocket
    //                if (httpPayload.IsTunnelConnectionRequested)
    //                {
    //                    try
    //                    {
    //                        await TunnelingHelper.AcceptTunnel(downStreamConnection).ConfigureAwait(false);

    //                        if (!await TunnelingHelper
    //                            .CheckForWebSocketRequest(downStreamConnection, httpPayload?.Uri.Host,
    //                                httpPayload.Uri.Port).ConfigureAwait(false))
    //                        {
    //                            // SSL Connection request 
    //                            if (_startupSetting.ShouldSkipDecryption(httpPayload.Uri.Host, httpPayload.Uri.Port))
    //                            {
    //                                return new ProxyMessage(httpPayload,
    //                                    new Destination(httpPayload.Uri.Host, httpPayload.Uri.Port,
    //                                        DestinationType.BlindSecure));
    //                            }

    //                            //anticipationTask =
    //                            //    _channelPoolManager.AnticipateSecureConnectionCreation(httpPayload?.Uri.Host,
    //                            //        httpPayload.Uri.Port);

    //                            // Decrypt host 

    //                            httpPayload.SslConnectionStart = _referenceClock.Instant();

    //                            await _secureConnectionUpdater
    //                                .Upgrade(downStreamConnection, httpPayload.Uri.Host, httpPayload.Uri.Port)
    //                                .ConfigureAwait(false);

    //                            httpPayload.SslConnectionEnd = _referenceClock.Instant();

    //                            downStreamConnection.IsSecure = true;
    //                        }
    //                        else
    //                        {
    //                            // WebSocket
    //                            // boucle
    //                        }
    //                    }
    //                    catch (EchoesException eex)
    //                    {
    //                        httpPayload.AddError(eex.Message, HttpProxyErrorType.NetworkError, eex.ToString());
    //                        return new ProxyMessage(httpPayload, null);
    //                    }
    //                }
    //                else
    //                {
    //                    httpPayload.DownStreamStartSendingHeader = headerResult.FirstByteReceived;
    //                    httpPayload.DownStreamCompleteSendingHeader = headerResult.LastByteReceived;

    //                    break;
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            if (ex is EchoesException)
    //            {
    //                // Une erreur pendant la lecture de la requêtes navigateur n'implique pas de logging. 
    //                if (httpPayload == null)
    //                    return new ProxyMessage(false);
    //            }

    //            throw;
    //        }

    //        var currentDestination = new Destination(
    //            httpPayload.Uri.Host, 
    //            httpPayload.Uri.Port, 
    //            downStreamConnection.IsSecure ? DestinationType.Secure : DestinationType.Insecure);

    //        try
    //        {
    //            if (!httpPayload.NoBody && ((httpPayload.ContentLength > 0)))
    //            {
    //                using (var bodyStream = new MemoryStream())
    //                {
    //                    if (httpPayload.ContentLength > 0)
    //                    {
    //                        var bodyResult = await clientStreamReader
    //                            .ReadBodyAsync(httpPayload.ContentLength, bodyStream)
    //                            .ConfigureAwait(false);

    //                        httpPayload.OnWireContentLength = bodyResult.Length;
    //                    }
    //                    else if (httpPayload.IsChunkedTransfert)
    //                    {
    //                        var bodyResult = await clientStreamReader
    //                            .ReadBodyChunkedAsync(bodyStream)
    //                            .ConfigureAwait(false);

    //                        httpPayload.OnWireContentLength = bodyResult.Length;
    //                    }
    //                    else
    //                    {
    //                        var bodyResult = await clientStreamReader
    //                            .ReadBodyUntilEofAsync(bodyStream)
    //                            .ConfigureAwait(false);

    //                        httpPayload.OnWireContentLength = bodyResult.Length;
    //                    }

    //                    httpPayload.Body = bodyStream.ToArray();
    //                }
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            if (ex is EchoesException eex)
    //            {
    //                httpPayload.AddError(eex.Message, HttpProxyErrorType.ClientError, eex.ToString());
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        finally
    //        {
    //            if (anticipationTask != null)
    //            {
    //                //await anticipationTask.ConfigureAwait(false);
    //            }
    //        }

    //        return new ProxyMessage(httpPayload, currentDestination);
    //    }
    //}
}