using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services
{

    public class SystemProxyState
    {
        public bool On { get; set;  }

        public string Address { get; set; } 

        public int Port { get; set; }
    }

    public class SystemProxyStateControl : ObservableProvider<SystemProxyState>
    {
        private readonly IObservable<FluxzySettingsHolder> _fluxzySettingHolderObservable;
        private FluxzySetting _fluxzySetting;

        public SystemProxyStateControl(IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable)
        {
            _fluxzySettingHolderObservable = fluxzySettingHolderObservable;
            var current = SystemProxyRegistration.GetSystemProxySetting();
            Subject = new BehaviorSubject<SystemProxyState>(new SystemProxyState()
            {
                Address = current.BoundHost,
                Port = current.ListenPort,
                On = current.Enabled
            });

            fluxzySettingHolderObservable.Do(t => this._fluxzySetting = t.StartupSetting); 
        }

        public override BehaviorSubject<SystemProxyState> Subject { get; }

        public void On()
        {

        }

        public void Off()
        {

        }

    }

    public class ProxyControl : ObservableProvider<ProxyState>
    {
        private readonly IHubContext<GlobalHub> _hub;
        private Proxy?  _proxy;
        private readonly BehaviorSubject<ProxyState> _internalSubject; 

        public ProxyControl(
            IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable,
            IObservable<FileContentOperationManager> contentObservable,
            IHubContext<GlobalHub> hub)
        {
            _hub = hub;
            _internalSubject = new BehaviorSubject<ProxyState>(new ProxyState());
            
            fluxzySettingHolderObservable
                .CombineLatest(
                    contentObservable
                        .DistinctUntilChanged(t => t.State.Identifier) // this will avoid unecessary refresh when 
                    ) 
                .Select(stateAndSetting =>
                    System.Reactive.Linq.Observable.Create<ProxyState>(
                        async (observer, _) =>
                        {
                            var setting = stateAndSetting.First.StartupSetting;

                            var trunkState = await stateAndSetting.Second.Observable.FirstAsync();

                            setting.ExchangeStartIndex = trunkState.MaxExchangeId;
                            setting.ArchivingPolicy = 
                                ArchivingPolicy.CreateFromDirectory(
                                    stateAndSetting.Second.State.WorkingDirectory    
                                    );

                            var proxyState = await ReloadProxy(setting, stateAndSetting.Second);

                            observer.OnNext(proxyState);
                            observer.OnCompleted();
                        }))
                .Switch()
                .Do(proxyState => 
                    _internalSubject.OnNext(proxyState)).Subscribe();

            Subject = _internalSubject;
        }
        
        private async Task<ProxyState> ReloadProxy(
            FluxzySetting fluxzySetting, 
            FileContentOperationManager currentContentOperationManager)
        {
            if (_proxy != null)
            {
                await _proxy.DisposeAsync();
                _proxy = null; 
            }
            
            _proxy = new Proxy(fluxzySetting,
                        new CertificateProvider(fluxzySetting,
                        new InMemoryCertificateCache()));

            _proxy.Writer.ExchangeUpdated += delegate (object? sender, ExchangeUpdateEventArgs args)
            {
                currentContentOperationManager.AddOrUpdate(args.ExchangeInfo);

                _hub.Clients.All.SendAsync(
                    "exchangeUpdate", args.ExchangeInfo);
            };

            _proxy.Writer.ConnectionUpdated += delegate(object? sender, ConnectionUpdateEventArgs args)
            {
                _hub.Clients.All.SendAsync(
                    "connectionUpdate", args.Connection);
            };

            _proxy.Run();

            return GetProxyState();
        }
        

        private ProxyState GetProxyState()
        {
            return new ProxyState()
            {
                IsListening = _proxy != null,
                BoundConnections = _proxy?.StartupSetting.BoundPoints
                    .Select(b => new ProxyEndPoint(b.Address, b.Port))
                    .ToList() ?? new List<ProxyEndPoint>()
            }; 
        }
        

        public override BehaviorSubject<ProxyState> Subject { get; }
    
    }
}