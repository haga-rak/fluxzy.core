// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Writers;

namespace Fluxzy.Tests.Tools;

public class AddHocConfigurableProxy : IAsyncDisposable
{
    private readonly int _expectedRequestCount;
    private readonly FluxzySetting _startupSetting;
    private readonly Proxy _proxy;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly List<Exchange> _capturedExchanges = new();
    private readonly TaskCompletionSource _completionSource;

    private int _requestCount = 0; 

    public AddHocConfigurableProxy(int expectedRequestCount = 1, int timeoutSeconds = 5)
    {
        _expectedRequestCount = expectedRequestCount;

        BindHost = "127.0.0.1";

        _startupSetting = FluxzySetting
            .CreateDefault()
            .SetBoundAddress(BindHost, BindPort);

        _proxy = new Proxy(_startupSetting,
            new CertificateProvider(_startupSetting,
                new InMemoryCertificateCache()));


        _proxy.Writer.ExchangeUpdated +=  ProxyOnBeforeResponse;

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

    }

    public IReadOnlyCollection<IPEndPoint> Run()
    {
        return _proxy.Run();
    }

    private void ProxyOnBeforeResponse(object? sender, ExchangeUpdateEventArgs exchangeUpdateEventArgs)
    {
        if (exchangeUpdateEventArgs.UpdateType != UpdateType.AfterResponseHeader)
            return; 

        lock (_capturedExchanges)
            _capturedExchanges.Add(exchangeUpdateEventArgs.Original);

        if (Interlocked.Increment(ref _requestCount) >= _expectedRequestCount)
        {
            _completionSource.TrySetResult();
        }
    }

    public int BindPort { get; }

    public string BindHost { get; }

    public ImmutableList<Exchange> CapturedExchanges => _capturedExchanges.ToImmutableList();

    public FluxzySetting StartupSetting => _startupSetting;

    public Task WaitUntilDone()
    {
        return _completionSource.Task; 
    }
    
    public async ValueTask DisposeAsync()
    {

        await _proxy.DisposeAsync();
        _cancellationSource.Dispose();
    }
}