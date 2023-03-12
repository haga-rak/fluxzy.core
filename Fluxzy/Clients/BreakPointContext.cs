// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules;

namespace Fluxzy.Clients
{
    public class BreakPointContext
    {
        private readonly int _exchangeId;
        private readonly Action<BreakPointContext> _statusChanged;
        private readonly TaskCompletionSource<IPEndPoint?> _endPointRequestCompletionSource;

        public BreakPointContext(int exchangeId, Action<BreakPointContext> statusChanged)
        {
            _exchangeId = exchangeId;
            _statusChanged = statusChanged;

            _endPointRequestCompletionSource = new TaskCompletionSource<IPEndPoint?>();
            OnEndPointRequest = _endPointRequestCompletionSource.Task;
        }

        public Task<IPEndPoint?>? OnEndPointRequest { get; set; }

        public bool SetEndPoint(IPEndPoint? endPoint)
        {
            return _endPointRequestCompletionSource.TrySetResult(endPoint);
        }
    }
}
