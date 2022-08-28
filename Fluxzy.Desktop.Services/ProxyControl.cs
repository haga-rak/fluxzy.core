using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class ProxyControl : IObservableProvider<ProxyState>
    {
        private Proxy?  _proxy;
        private readonly BehaviorSubject<ProxyState> _internalSubject;  

        public ProxyControl(IObservable<FluxzySettingsHolder> fluxzySettingHolder)
        {
            _internalSubject = new BehaviorSubject<ProxyState>(new ProxyState());

            _internalSubject.Do(p => Current = p).Subscribe();

            fluxzySettingHolder
                .Select(m =>
                    System.Reactive.Linq.Observable.Create<ProxyState>(
                        async (observer, token) =>
                        {
                            var proxyState = await UpdateSettings(m);
                            observer.OnNext(proxyState);
                            observer.OnCompleted();
                        }))
                .Switch()
                .Do(proxyState => 
                    _internalSubject.OnNext(proxyState)).Subscribe();

            Observable = _internalSubject.AsObservable();
        }
        
        private async Task<ProxyState> UpdateSettings(FluxzySettingsHolder settingHolder)
        {
            if (_proxy != null)
            {
                await _proxy.DisposeAsync();
                _proxy = null; 
            }

            var currentSettingHolder = settingHolder;

            _proxy = new Proxy(currentSettingHolder.StartupSetting,
                new CertificateProvider(currentSettingHolder.StartupSetting, new InMemoryCertificateCache()));

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