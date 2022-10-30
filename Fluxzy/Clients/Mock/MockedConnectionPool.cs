// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.Mock
{
    public class MockedConnectionPool : IHttpConnectionPool
    {
        private readonly PreMadeResponse _preMadeResponse;

        public MockedConnectionPool(Authority authority, PreMadeResponse preMadeResponse)
        {
            Authority = authority;
            _preMadeResponse = preMadeResponse;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }

        public Authority Authority { get; }

        public bool Complete { get; private set; }

        public void Init()
        {
        }

        public ValueTask<bool> CheckAlive()
        {
            return new ValueTask<bool>(Complete);
        }

        public async ValueTask Send(Exchange exchange,
            ILocalLink localLink, RsBuffer buffer,
            CancellationToken cancellationToken = default)
        {
            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();
            exchange.Metrics.TotalSent = 0;

            if (exchange.Request.Body != null)
                await exchange.Request.Body.DrainAsync(); // We empty request body stream 

            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            exchange.Response.Header = new ResponseHeader(
                _preMadeResponse.GetFlatH11Header(Authority).AsMemory(),
                exchange.Authority.Secure);

            exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant();
            exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();

            exchange.Response.Body =
                new MetricsStream(_preMadeResponse.ReadBody(Authority),
                    () => { exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant(); },
                    length =>
                    {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.Metrics.TotalReceived += length;
                        exchange.ExchangeCompletionSource.SetResult(true);
                    },
                    exception =>
                    {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.ExchangeCompletionSource.SetException(exception);
                    },
                    cancellationToken
                )
                ;

            Complete = true;
        }

        public void Dispose()
        {
        }
    }
}
