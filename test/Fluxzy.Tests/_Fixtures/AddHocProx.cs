// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Writers;

namespace Fluxzy.Tests._Fixtures
{
    public class AddHocProxy : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationSource;
        private readonly List<Exchange> _capturedExchanges = new();
        private readonly TaskCompletionSource _completionSource;
        private readonly int _expectedRequestCount;
        private readonly Proxy _proxy;
        private readonly FluxzySetting _startupSetting;

        private int _requestCount;

        public AddHocProxy(int expectedRequestCount = 1, int timeoutSeconds = 5, Action<FluxzySetting>? configureSetting = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                timeoutSeconds = timeoutSeconds * 5; 

            _expectedRequestCount = expectedRequestCount;

            BindHost = "127.0.0.1";

            _startupSetting = FluxzySetting
                              .CreateDefault()
                              .SetBoundAddress(BindHost, BindPort);

            configureSetting?.Invoke(_startupSetting);

            _proxy = new Proxy(_startupSetting,
                new CertificateProvider(_startupSetting.CaCertificate,
                    new InMemoryCertificateCache()), new DefaultCertificateAuthorityManager());

            _proxy.Writer.ExchangeUpdated += ProxyOnBeforeResponse;

            _cancellationSource = new CancellationTokenSource(timeoutSeconds * 1000);
            _completionSource = new TaskCompletionSource();

            _cancellationSource.Token.Register(() => {
                if (!_completionSource.Task.IsCompleted) {
                    _completionSource.TrySetException(
                        new TimeoutException($"Timeout of {timeoutSeconds} seconds reached"));
                }
            });

            var endPoints = _proxy.Run();

            var endPoint = endPoints.First();

            BindPort = endPoint.Port;
        }

        public int BindPort { get; }

        public string BindHost { get; }

        public ImmutableList<Exchange> CapturedExchanges => _capturedExchanges.ToImmutableList();

        public async ValueTask DisposeAsync()
        {
            await _proxy.DisposeAsync();
            _cancellationSource.Dispose();
        }

        private void ProxyOnBeforeResponse(object? sender, ExchangeUpdateEventArgs exchangeUpdateEventArgs)
        {
            if (exchangeUpdateEventArgs.UpdateType != ArchiveUpdateType.AfterResponseHeader)
                return;

            lock (_capturedExchanges) {
                _capturedExchanges.Add(exchangeUpdateEventArgs.Original);
            }

            if (Interlocked.Increment(ref _requestCount) >= _expectedRequestCount)
                _completionSource.TrySetResult();
        }

        public Task WaitUntilDone()
        {
            return _completionSource.Task;
        }
    }
}
