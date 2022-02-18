using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core;
using Echoes.H2.Tests.Utils;

namespace Echoes.H2.Tests.Tools
{
    public class AddHocProxy : IDisposable
    {
        private readonly int _portNumber;
        private readonly int _expectedRequestCount;
        private readonly ProxyStartupSetting _startupSetting;
        private readonly Proxy _proxy;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly List<Exchange> _capturedExchanges = new();
        private readonly TaskCompletionSource _completionSource;

        private int _requestCount = 0; 

        public AddHocProxy(int portNumber, int expectedRequestCount = 1, int timeoutSeconds = 5)
        {
            _portNumber = portNumber;
            _expectedRequestCount = expectedRequestCount;

            BindHost = "127.0.0.1";
            BindPort = portNumber;

            _startupSetting = ProxyStartupSetting
                .CreateDefault()
                .SetAsSystemProxy(false)
                .SetBoundAddress(BindHost)
                .SetListenPort(BindPort);

            _proxy = new Proxy(_startupSetting,
                new CertificateProvider(_startupSetting,
                    new InMemoryCertificateCache()),
                OnNewExchange);

            _cancellationSource = new CancellationTokenSource(timeoutSeconds * 1000);
            _completionSource = new TaskCompletionSource();

            _cancellationSource.Token.Register(() =>
            {
                if (!_completionSource.Task.IsCompleted)
                {
                    _completionSource.TrySetException(
                        new TimeoutException($"Timeout of {timeoutSeconds} seconds reached"));
                }
            }); 

            _proxy.Run();
        }

        public int BindPort { get; }

        public string BindHost { get; }

        public ImmutableList<Exchange> CapturedExchanges => _capturedExchanges.ToImmutableList();

        private Task OnNewExchange(Exchange exchange)
        {
            lock (_capturedExchanges)
                _capturedExchanges.Add(exchange);

            if (Interlocked.Increment(ref _requestCount) >= _expectedRequestCount)
            {
                _completionSource.TrySetResult();
            }

            return Task.CompletedTask; 
        }

        public Task WaitUntilDone()
        {
            return _completionSource.Task; 
        }

        public void Dispose()
        {
            _proxy.Dispose();
            _cancellationSource.Dispose();
        }
    }
}

