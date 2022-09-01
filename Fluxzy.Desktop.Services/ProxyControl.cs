using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services
{
    public class ProxyControl : IObservableProvider<ProxyState>
    {
        private readonly IHubContext<GlobalHub> _hub;
        private Proxy?  _proxy;
        private readonly BehaviorSubject<ProxyState> _internalSubject;  

        public ProxyControl(
            IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable,
            IObservable<FileState> fileStateObservable,
            IHubContext<GlobalHub> hub)
        {
            _hub = hub;
            _internalSubject = new BehaviorSubject<ProxyState>(new ProxyState());
            
            _internalSubject.Do(p => Current = p).Subscribe();
            
            fluxzySettingHolderObservable
                .CombineLatest(fileStateObservable)
                .Select(stateAndSetting =>
                    System.Reactive.Linq.Observable.Create<ProxyState>(
                        async (observer, token) =>
                        {
                            var setting = stateAndSetting.First.StartupSetting;

                            setting.RegisterAsSystemProxy = Current?.IsListening ?? false;

                            setting.ArchivingPolicy = 
                                ArchivingPolicy.CreateFromDirectory(
                                    stateAndSetting.Second.WorkingDirectory
                                );

                            var proxyState = await ReloadProxy(setting);

                            observer.OnNext(proxyState);
                            observer.OnCompleted();
                        }))
                .Switch()
                .Do(proxyState => 
                    _internalSubject.OnNext(proxyState)).Subscribe();

            Observable = _internalSubject.AsObservable();
        }
        
        private async Task<ProxyState> ReloadProxy(FluxzySetting fluxzySetting)
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
                // TODO notify TrunkManager for this update 

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

        public Task<bool> SetAsSystemProxy()
        {
            if (_proxy == null)
                return Task.FromResult(false); 

            _proxy.SetAsSystemProxy();

            _internalSubject.OnNext(GetProxyState());

            return Task.FromResult(true); 
        }

        public Task<bool> UnsetAsSystemProxy()
        {
            if (_proxy == null)
                return Task.FromResult(false);

            _proxy.UnsetAsSystemProxy();

            _internalSubject.OnNext(GetProxyState());

            return Task.FromResult(true);
        }

        private ProxyState GetProxyState()
        {
            return new ProxyState()
            {
                IsListening = _proxy != null && _proxy.SystemProxyOn,
                IsSystemProxyOn = _proxy != null,
                BoundConnections = _proxy?.StartupSetting.BoundPoints
                    .Select(b => new ProxyEndPoint(b.Address, b.Port))
                    .ToList() ?? new List<ProxyEndPoint>()
            }; 
        }

        public ProxyState? Current { get; private set; }

        public IObservable<ProxyState> Observable { get; }
    
    }
}