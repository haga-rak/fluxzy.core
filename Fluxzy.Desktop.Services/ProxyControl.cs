using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Extensions;
using Fluxzy.Interop.Pcap;
using Fluxzy.Readers;
using Fluxzy.Rules;
using Fluxzy.Writers;

namespace Fluxzy.Desktop.Services
{
    public class ProxyControl : ObservableProvider<ProxyState>
    {
        private readonly ProxyScope _proxyScope;
        private readonly FileContentUpdateManager _fileContentUpdateManager;
        private readonly BehaviorSubject<ProxyState> _internalSubject;
        private readonly BehaviorSubject<RealtimeArchiveWriter?> _writerSubject = new(null);
        private Proxy? _proxy;
        private ITcpConnectionProvider?  _tcpConnectionProvider;

        public IObservable<RealtimeArchiveWriter?> WriterObservable { get; }

        protected override BehaviorSubject<ProxyState> Subject { get; }

        public ProxyControl(
            ProxyScope proxyScope,
            IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable,
            IObservable<FileContentOperationManager> contentObservable,
            IObservable<ViewFilter> viewFilter,
            IObservable<List<Rule>> activeRuleObservable,
            IObservable<IArchiveReader> archiveReaderObservable,
            FileContentUpdateManager fileContentUpdateManager)
        {
            _proxyScope = proxyScope;
            _fileContentUpdateManager = fileContentUpdateManager;

            _internalSubject = new BehaviorSubject<ProxyState>(new ProxyState
            {
                OnError = true,
                Message = "Not started yet"
            });

            fluxzySettingHolderObservable
                .CombineLatest(
                    contentObservable
                        .DistinctUntilChanged(t => t.State.Identifier), // this will avoid unecessary refresh  
                    activeRuleObservable
                )
                .Select(stateAndSetting =>
                    Observable.Create<ProxyState>(
                        async (observer, _) =>
                        {
                            var setting = stateAndSetting.First.StartupSetting;

                            var trunkState = await stateAndSetting.Second.Observable.FirstAsync();

                            setting.ArchivingPolicy =
                                ArchivingPolicy.CreateFromDirectory(
                                    stateAndSetting.Second.State.WorkingDirectory
                                );

                            setting.AlterationRules = stateAndSetting.Third;

                            var proxyState = await ReloadProxy(
                                setting, stateAndSetting.Second,
                                trunkState.MaxConnectionId, trunkState.MaxExchangeId);

                            observer.OnNext(proxyState);
                            observer.OnCompleted();
                        }))
                .Switch()
                .Do(proxyState => _internalSubject.OnNext(proxyState)).Subscribe();

            Subject = _internalSubject;
            WriterObservable = _writerSubject.AsObservable();

            viewFilter
                .Do(v => { })
                .Subscribe();

            archiveReaderObservable.Do(a => ArchiveReader = a).Subscribe();
        }

        public IArchiveReader? ArchiveReader { get; private set; }

        private async Task<ProxyState> ReloadProxy(
            FluxzySetting fluxzySetting,
            FileContentOperationManager currentContentOperationManager, int maxConnectionId, int maxExchangeId)
        {
            if (_proxy != null)
            {
                await _proxy.DisposeAsync();
                _proxy = null;
                await _tcpConnectionProvider!.DisposeAsync();
                _tcpConnectionProvider = null; 
            }

            IEnumerable<IPEndPoint> endPoints;

            try
            {
                _tcpConnectionProvider =
                    fluxzySetting.CaptureRawPacket
                        ? await CapturedTcpConnectionProvider.Create(_proxyScope, fluxzySetting)
                        : ITcpConnectionProvider.Default;

                _proxy = new Proxy(fluxzySetting, 
                    new CertificateProvider(fluxzySetting, new InMemoryCertificateCache()), new DefaultCertificateAuthorityManager(),
                    _tcpConnectionProvider, new UaParserUserAgentInfoProvider());

                _proxy.IdProvider.SetNextConnectionId(maxConnectionId);
                _proxy.IdProvider.SetNextExchangeId(maxExchangeId);

                _writerSubject.OnNext(_proxy.Writer);

                _proxy.Writer.ExchangeUpdated += delegate(object? _, ExchangeUpdateEventArgs args)
                {
                    _fileContentUpdateManager.AddOrUpdate(args.ExchangeInfo, ArchiveReader!);
                };

                _proxy.Writer.ConnectionUpdated += delegate(object? _, ConnectionUpdateEventArgs args)
                {
                    _fileContentUpdateManager.AddOrUpdate(args.Connection);
                };

                endPoints = _proxy.Run();
            }
            catch (Exception ex)
            {
                if (_proxy != null)
                {
                    _writerSubject.OnNext(null);
                    await _proxy.DisposeAsync();
                    await _tcpConnectionProvider!.DisposeAsync();
                }

                return new ProxyState
                {
                    OnError = true,
                    Message = ex.Message
                };
            }

            return GetProxyState(endPoints);
        }

        private ProxyState GetProxyState(IEnumerable<IPEndPoint> endPoints)
        {
            return new ProxyState
            {
                BoundConnections = endPoints
                                   .Select(b => new ProxyEndPoint(b.Address.ToString(), b.Port))
                                   .ToList() ?? new List<ProxyEndPoint>()
            };
        }

        public bool TryFlush()
        {
            if (_tcpConnectionProvider == null)
                return false;

            if (_tcpConnectionProvider is CapturedTcpConnectionProvider capturedTcpConnectionProvider 
                && capturedTcpConnectionProvider.CaptureContext != null) {
                capturedTcpConnectionProvider.CaptureContext.Flush();
                return true;
            }

            return false;
        }
    }
}
