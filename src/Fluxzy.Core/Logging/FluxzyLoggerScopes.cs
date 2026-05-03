// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections;
using System.Collections.Generic;
using Fluxzy.Core;
using Microsoft.Extensions.Logging;

namespace Fluxzy.Logging
{
    internal static class FluxzyLoggerScopes
    {
        public static IDisposable? BeginConnectionScope(
            ILogger logger, long proxyConnectionId, string downstreamRemote, string downstreamLocal)
        {
            return logger.BeginScope(new ConnectionScopeState(proxyConnectionId, downstreamRemote, downstreamLocal));
        }

        public static IDisposable? BeginExchangeScope(ILogger logger, Exchange exchange)
        {
            return logger.BeginScope(new ExchangeScopeState(
                exchange.Id,
                exchange.Authority.ToString(),
                exchange.Method,
                exchange.Path,
                exchange.HttpVersion));
        }
    }

    internal sealed class ConnectionScopeState : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly long _proxyConnectionId;
        private readonly string _downstreamRemote;
        private readonly string _downstreamLocal;

        public ConnectionScopeState(long proxyConnectionId, string downstreamRemote, string downstreamLocal)
        {
            _proxyConnectionId = proxyConnectionId;
            _downstreamRemote = downstreamRemote;
            _downstreamLocal = downstreamLocal;
        }

        public int Count => 3;

        public KeyValuePair<string, object?> this[int index] => index switch {
            0 => new KeyValuePair<string, object?>("ProxyConnectionId", _proxyConnectionId),
            1 => new KeyValuePair<string, object?>("DownstreamRemote", _downstreamRemote),
            2 => new KeyValuePair<string, object?>("DownstreamLocal", _downstreamLocal),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return $"ProxyConnectionId={_proxyConnectionId} DownstreamRemote={_downstreamRemote} DownstreamLocal={_downstreamLocal}";
        }
    }

    internal sealed class ExchangeScopeState : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly int _exchangeId;
        private readonly string _authority;
        private readonly string _method;
        private readonly string _path;
        private readonly string _httpVersion;

        public ExchangeScopeState(int exchangeId, string authority, string method, string path, string httpVersion)
        {
            _exchangeId = exchangeId;
            _authority = authority;
            _method = method;
            _path = path;
            _httpVersion = httpVersion;
        }

        public int Count => 5;

        public KeyValuePair<string, object?> this[int index] => index switch {
            0 => new KeyValuePair<string, object?>("ExchangeId", _exchangeId),
            1 => new KeyValuePair<string, object?>("Authority", _authority),
            2 => new KeyValuePair<string, object?>("Method", _method),
            3 => new KeyValuePair<string, object?>("Path", _path),
            4 => new KeyValuePair<string, object?>("HttpVersion", _httpVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return $"ExchangeId={_exchangeId} Authority={_authority} Method={_method} Path={_path} HttpVersion={_httpVersion}";
        }
    }
}
