// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Logging;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.H11
{
    internal class Http11PoolProcessing
    {
        private readonly TimeSpan _expectContinueTimeout;
        private readonly ILogger _logger;

        public Http11PoolProcessing(TimeSpan expectContinueTimeout, ILogger? logger = null)
        {
            _expectContinueTimeout = expectContinueTimeout;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        ///     Process the exchange
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="buffer"></param>
        /// <param name="exchangeScope"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if remote server close connection</returns>
        public async ValueTask<bool> Process(Exchange exchange, RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken cancellationToken)
        {
            if (exchange.Context.EventNotifierStream?.Faulted == true) {
                throw new ConnectionCloseException("Abandoned stream");
            }

            exchange.Connection!.AddNewRequestProcessed();

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            var headerLength = exchange.Request.Header.WriteHttp11(
                !exchange.Authority.Secure,
                buffer, skipNonForwardableHeader: true, writeExtraHeaderField: true, requestClose: false);

            exchange.Metrics.RequestHeaderLength = headerLength;

            FluxzyLogEvents.LogRequestSending(_logger, exchange);

            // Sending request header

            await exchange.Connection.WriteStream!
                          .WriteAsync(buffer.Memory.Slice(0, headerLength), cancellationToken)
                          .ConfigureAwait(false);

            exchange.Metrics.TotalSent += headerLength;
            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            // Expect: 100-continue path — read any interim/final response from
            // upstream before streaming the body, otherwise the body-copy below
            // would block on a client that is itself waiting for our 100 Continue
            // (deadlock — issue #624).

            HeaderBlockReadResult headerBlockDetectResult = default;
            var hasEarlyResponseHeader = false;

            if (exchange.Request.Header.HasExpectContinue && _expectContinueTimeout > TimeSpan.Zero) {
                headerBlockDetectResult = await ReadExpectContinueInterim(
                    exchange, buffer, cancellationToken).ConfigureAwait(false);

                if (headerBlockDetectResult.HeaderLength > 0) {
                    var earlyStatus = HttpHelper.ReadStatusCode(
                        buffer.Buffer.AsSpan(0, headerBlockDetectResult.HeaderLength));

                    if (earlyStatus >= 100 && earlyStatus < 200) {
                        // 1xx interim — forward verbatim to client, then keep
                        // pumping the body as usual. Upstream will send the
                        // final response later; the regular response-read loop
                        // will pick it up.
                        await ForwardInterimToClient(exchange, earlyStatus, cancellationToken)
                              .ConfigureAwait(false);
                    }
                    else {
                        // Final response before body — the origin rejected
                        // (413, 417, 401…) or produced a response without
                        // needing the body. Skip body send entirely and hand
                        // the already-buffered header off to the normal
                        // response-processing tail.
                        hasEarlyResponseHeader = true;
                    }
                }
                else {
                    // Origin stayed silent — either it doesn't honour Expect
                    // (HTTP/1.0, naive origins) or the timeout beat the
                    // network. Synthesise a 100 Continue to the client so it
                    // releases the body (nginx/Apache do the same). Without
                    // this, the body pump below would block forever on a
                    // client that's still waiting for the interim response.
                    await ForwardInterimToClient(exchange, 100, cancellationToken)
                          .ConfigureAwait(false);
                }
            }

            // Sending request body (unless the origin already answered)

            if (!hasEarlyResponseHeader && exchange.Request.Body != null) {
                var writeStream = exchange.Connection.WriteStream;
                ChunkedTransferWriteStream? chunkedStream = null;

                if (exchange.Request.Header.ChunkedBody) {
                    writeStream = chunkedStream = new ChunkedTransferWriteStream(writeStream);
                }

                var totalBodySize = await
                    exchange.Request.Body.CopyDetailed(writeStream, 1024 * 16,
                        _ => { }, cancellationToken).ConfigureAwait(false);

                exchange.Metrics.TotalSent += totalBodySize;

                if (chunkedStream != null) {
                    await chunkedStream.WriteEof().ConfigureAwait(false);
                }

                exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();
            }
            else {
                exchange.Metrics.RequestBodySent = exchange.Metrics.RequestHeaderSent;
            }

            FluxzyLogEvents.LogRequestSent(_logger, exchange, hasEarlyResponseHeader);

            // Waiting for header block — unless the Expect pre-read already
            // produced the final response header.

            if (!hasEarlyResponseHeader) {
                try {
                    while (true) {
                        headerBlockDetectResult = await Http11HeaderBlockReader.GetNext(exchange.Connection.ReadStream!,
                            buffer,
                            () => exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant(),
                            () => exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant(),
                            true,
                            cancellationToken,
                            true).ConfigureAwait(false);

                        var earlyStatus = HttpHelper.ReadStatusCode(
                            buffer.Buffer.AsSpan(0, headerBlockDetectResult.HeaderLength));

                        if (earlyStatus >= 100 && earlyStatus < 200) {
                            // Forward any interim response to the client (previously
                            // only 100 Continue was silently dropped). Apache-style
                            // origins can send a 100 here even without Expect:
                            // that still needs to reach the client per RFC 9110.
                            await ForwardInterimToClient(exchange, earlyStatus, cancellationToken)
                                  .ConfigureAwait(false);
                            continue;
                        }

                        break;
                    }
                }
                catch (Exception ex) {
                    // A read failure on a connection that came from the pool, before any
                    // response byte has been seen, means the upstream tore the connection
                    // down while it was idle (TLS close_notify + FIN, or an outright RST).
                    // The request never reached the origin — relaunching on a fresh
                    // connection is safe regardless of HTTP method. The recycled-and-no-
                    // response gate is what keeps a genuine fresh-connection failure
                    // (server closes before responding) from looping forever.

                    var noResponseByteYet = exchange.Metrics.ResponseHeaderStart == default;
                    var recycledAndDead = exchange.RecycledConnection && noResponseByteYet;

                    if (ex is TlsFatalAlert
                        || (exchange.Context.EventNotifierStream?.Faulted ?? false)
                        || (recycledAndDead && (ex is IOException || ex is SocketException))) {
                        throw new ConnectionCloseException("Relaunch");
                    }

                    throw new ClientErrorException(0,
                        "The connection was closed while trying to read the response header",
                        ex.Message, ex);
                }
            }

            if (headerBlockDetectResult.CloseNotify) {
                throw new ConnectionCloseException("Relaunch");
            }

            Memory<char> headerContent = exchangeScope.RegisterForReturn(headerBlockDetectResult.HeaderLength);

            Encoding.ASCII
                    .GetChars(buffer.Memory.Slice(0, headerBlockDetectResult.HeaderLength).Span, headerContent.Span);

            exchange.Response.Header = new ResponseHeader(
                headerContent, exchange.Authority.Secure, true);

            exchange.Metrics.TotalReceived += headerBlockDetectResult.HeaderLength;
            exchange.Metrics.ResponseHeaderLength = headerBlockDetectResult.HeaderLength;

            FluxzyLogEvents.LogResponseHeaderReceived(_logger, exchange);

            var shouldCloseConnection = exchange.Response.Header.ConnectionCloseRequest;

            if (!exchange.Response.Header.HasResponseBody(exchange.Request.Header.Method.Span, out var shouldClose)) {
                // We close the connection because
                // many web server still sends a content-body with a 304 response
                // https://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html 10.3.5

                exchange.Metrics.ResponseBodyStart =
                    exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                exchange.Response.Body = Stream.Null;

                exchange.ExchangeCompletionSource.TrySetResult(shouldCloseConnection || shouldClose);
                
                return true;
            }

            if (exchange.Response.Header.ConnectionCloseRequest &&
                exchange.Response.Header.ContentLength == -1 &&
                !exchange.Response.Header.ChunkedBody) {

                // we force chunked transfer encoding when the server
                // does not send a content-length header and request a close connection

                exchange.ReadUntilClose = true;
            }

            var bodyStream = exchange.Connection.ReadStream!;

            if (headerBlockDetectResult.HeaderLength < headerBlockDetectResult.TotalReadLength) {
                var length = headerBlockDetectResult.TotalReadLength -
                             headerBlockDetectResult.HeaderLength;

                // Concat the extra body bytes read while retrieving header
                bodyStream = new CombinedReadonlyStream(
                    shouldCloseConnection,
                    buffer.Buffer.AsSpan(headerBlockDetectResult.HeaderLength, length),
                    exchange.Connection.ReadStream!
                );
            }

            ChunkedTransferReadStream? chunkedReadStream = null;

            if (exchange.Response.Header.ChunkedBody) {
                chunkedReadStream = new ChunkedTransferReadStream(bodyStream, shouldCloseConnection);
                bodyStream = chunkedReadStream;
            }

            if (exchange.Response.Header.ContentLength > 0) {
                bodyStream = new ContentBoundStream(bodyStream, exchange.Response.Header.ContentLength);
            }

            exchange.Response.Body = new MetricsStream(bodyStream,
                    () => {
                        exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                    },
                    (endConnection, length) => {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.Metrics.TotalReceived += length;

                        if (chunkedReadStream?.Trailers != null)
                            exchange.Response.Trailers = chunkedReadStream.Trailers;

                        exchange.ExchangeCompletionSource.TrySetResult(endConnection);
                    },
                    exception => {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.ExchangeCompletionSource.SetException(exception);
                    },
                    shouldCloseConnection,
                    exchange.Response.Header.ContentLength >= 0 ? exchange.Response.Header.ContentLength : null,
                    cancellationToken
                );

            return shouldCloseConnection;
        }

        /// <summary>
        ///     Tries to read a response-header block from upstream within
        ///     <see cref="_expectContinueTimeout"/>. Returns default
        ///     (HeaderLength == 0) if the timeout fires before any bytes
        ///     arrive, so the caller can fall back to the legacy "send body,
        ///     then read response" flow.
        /// </summary>
        private async ValueTask<HeaderBlockReadResult> ReadExpectContinueInterim(
            Exchange exchange, RsBuffer buffer, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_expectContinueTimeout);

            try {
                return await Http11HeaderBlockReader.GetNext(
                    exchange.Connection!.ReadStream!,
                    buffer,
                    () => exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant(),
                    null,
                    throwOnError: false,
                    timeoutCts.Token,
                    dontThrowIfEarlyClosed: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                // Timeout — origin didn't produce 100 Continue in time, or
                // doesn't honour Expect at all. Fall back to legacy flow.
                return default;
            }
            catch (IOException) {
                // Socket-level read error — let the regular response loop
                // surface it with its richer error mapping.
                return default;
            }
        }

        private static async ValueTask ForwardInterimToClient(
            Exchange exchange, int statusCode, CancellationToken cancellationToken)
        {
            var writer = exchange.InterimResponseWriter;

            if (writer == null) {
                return; // no downstream bridge (H2, tunnel, legacy call site)
            }

            var reasonPhrase = Http11Constants
                .GetStatusLine(statusCode.ToString().AsMemory());

            await writer(statusCode, reasonPhrase, cancellationToken).ConfigureAwait(false);
        }
    }
}
