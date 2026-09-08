// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class ExchangeConnectionDispositionTests
    {
        [Fact]
        public async Task RegisteredDisposition_RemainsObservableAfterResponseCompletion()
        {
            var exchange = CreateExchange();
            var disposition = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            exchange.RegisterConnectionDisposition(disposition.Task);

            exchange.ExchangeCompletionSource.SetResult(false);

            Assert.True(exchange.Complete.IsCompletedSuccessfully);
            Assert.False(exchange.ConnectionDisposition.IsCompleted);

            disposition.SetResult();
            await exchange.ConnectionDisposition;
        }

        [Fact]
        public void Disposition_CanOnlyBeRegisteredOnce()
        {
            var exchange = CreateExchange();
            exchange.RegisterConnectionDisposition(Task.CompletedTask);

            Assert.Throws<InvalidOperationException>(
                () => exchange.RegisterConnectionDisposition(Task.CompletedTask));
        }

        private static Exchange CreateExchange()
        {
            var authority = new Authority("localhost", 443, true);
            return new Exchange(
                IIdProvider.FromZero, authority,
                "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(),
                "HTTP/1.1", DateTime.UtcNow);
        }
    }
}
