// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ExchangeEnvelopeRedactionTests
    {
        [Fact]
        public async Task Envelope_Redacts_Authorization_By_Default()
        {
            var factory = new TestLoggerFactory(LogLevel.Trace);

            await using var host = await InProcessHost.Create();

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            await using var proxy = new Proxy(setting, loggerFactory: factory);
            var endPoint = proxy.Run().First();

            using var client = Socks5ClientFactory.Create(endPoint);
            client.BaseAddress = new Uri(host.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer secret-token-12345");

            var response = await client.GetAsync("/hello");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await response.Content.ReadAsByteArrayAsync();
            response.Dispose();

            var envelope = await WaitForEvent(factory, eventId: 1099);
            Assert.NotNull(envelope);

            var requestHeaders = envelope.Properties.GetValueOrDefault("RequestHeaders")?.ToString();
            Assert.NotNull(requestHeaders);
            Assert.Contains("Authorization=", requestHeaders);
            Assert.Contains("<redacted, len=", requestHeaders);
            Assert.DoesNotContain("secret-token-12345", requestHeaders);
        }

        [Fact]
        public async Task Envelope_Includes_Authorization_When_Opted_In()
        {
            var factory = new TestLoggerFactory(LogLevel.Trace);

            await using var host = await InProcessHost.Create();

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            // Opt in to raw values via reflection (the property has internal set).
            typeof(FluxzySetting).GetProperty(nameof(FluxzySetting.LogIncludeSensitiveHeaders))!
                .SetValue(setting, true);

            await using var proxy = new Proxy(setting, loggerFactory: factory);
            var endPoint = proxy.Run().First();

            using var client = Socks5ClientFactory.Create(endPoint);
            client.BaseAddress = new Uri(host.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer secret-token-12345");

            var response = await client.GetAsync("/hello");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await response.Content.ReadAsByteArrayAsync();
            response.Dispose();

            var envelope = await WaitForEvent(factory, eventId: 1099);
            Assert.NotNull(envelope);

            var requestHeaders = envelope.Properties.GetValueOrDefault("RequestHeaders")?.ToString();
            Assert.NotNull(requestHeaders);
            Assert.Contains("Bearer secret-token-12345", requestHeaders);
        }

        private static async Task<CapturedLog?> WaitForEvent(TestLoggerFactory factory, int eventId)
        {
            for (var i = 0; i < 50; i++) {
                var match = factory.Logger.Logs.FirstOrDefault(l => l.EventId.Id == eventId);
                if (match != null) return match;
                await Task.Delay(50);
            }
            return null;
        }
    }

    internal sealed class CapturedLog
    {
        public LogLevel Level { get; init; }
        public EventId EventId { get; init; }
        public string Message { get; init; } = string.Empty;
        public Dictionary<string, object?> Properties { get; init; } = new();
    }

    internal sealed class TestLogger : ILogger
    {
        private readonly LogLevel _minLevel;

        public TestLogger(LogLevel minLevel)
        {
            _minLevel = minLevel;
        }

        public ConcurrentBag<CapturedLog> Logs { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var props = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> kvs) {
                foreach (var kv in kvs) {
                    props[kv.Key] = kv.Value;
                }
            }

            Logs.Add(new CapturedLog {
                Level = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Properties = props
            });
        }
    }

    internal sealed class TestLoggerFactory : ILoggerFactory
    {
        public TestLogger Logger { get; }

        public TestLoggerFactory(LogLevel minLevel)
        {
            Logger = new TestLogger(minLevel);
        }

        public ILogger CreateLogger(string categoryName) => Logger;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }
}
