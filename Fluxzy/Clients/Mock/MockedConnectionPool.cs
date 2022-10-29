// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.Mock
{
    public class MockedConnectionPool : IHttpConnectionPool
    {
        private readonly Authority _authority;
        private readonly PreMadeResponse _preMadeResponse;
        private bool _complete; 

        public MockedConnectionPool( Authority authority, PreMadeResponse preMadeResponse)
        {
            _authority = authority;
            _preMadeResponse = preMadeResponse;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public Authority Authority => _authority;

        public bool Complete => _complete;

        public void Init()
        {
        }

        public ValueTask<bool> CheckAlive()
        {
            return new ValueTask<bool>(_complete); 
        }

        public async ValueTask Send(Exchange exchange,
            ILocalLink localLink, RsBuffer buffer,
            CancellationToken cancellationToken = default)
        {
            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();
            exchange.Metrics.TotalSent = 0;

            if (exchange.Request.Body != null)
                await exchange.Request.Body.DrainAsync();  // We empty request body stream 

            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();
            
            exchange.Response.Header = new ResponseHeader(
                _preMadeResponse.GetFlatH11Header(_authority).AsMemory(), 
                exchange.Authority.Secure);

            exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant();
            exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();

            exchange.Response.Body =
                new MetricsStream(_preMadeResponse.ReadBody(_authority),
                    () =>
                    {
                        exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                    },
                    (length) =>
                    {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.Metrics.TotalReceived += length;
                        exchange.ExchangeCompletionSource.SetResult(true);
                      
                    },
                    (exception) =>
                    {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.ExchangeCompletionSource.SetException(exception);
                    },
                    cancellationToken
                )
                ;

            _complete = true; 
        }
    }
}