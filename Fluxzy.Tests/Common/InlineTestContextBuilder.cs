using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Extensions;
using Fluxzy.Writers;

namespace Fluxzy.Tests.Common
{
    internal static class InlineTestContextBuilder
    {
        public static (HttpClient, Proxy p) CreateTestContext(string bindHost, int timeoutSeconds,
            TaskCompletionSource<Exchange> requestReceived, FluxzySetting startupSetting,
            out CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource = new CancellationTokenSource(timeoutSeconds * 1000);

            cancellationTokenSource.Token.Register(() =>
            {
                if (!requestReceived.Task.IsCompleted)
                    requestReceived.SetException(new Exception("Response not received under {timeoutSeconds} seconds"));
            });

            var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting,
                    new FileSystemCertificateCache(startupSetting)), new DefaultCertificateAuthorityManager(),
                userAgentProvider: new UaParserUserAgentInfoProvider());

            proxy.Writer.ExchangeUpdated += delegate(object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };

            var endPoint = proxy.Run().First();

            var messageHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{bindHost}:{endPoint.Port}")
            };

            var httpClient = new HttpClient(messageHandler);

            return (httpClient, proxy);
        }
    }
}