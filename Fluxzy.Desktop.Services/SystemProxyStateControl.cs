using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class SystemProxyStateControl : ObservableProvider<SystemProxyState>
    {
        private readonly SystemProxyRegistrationManager _systemProxyRegistrationManager;
        private FluxzySetting? _fluxzySetting;
        private ProxyState? _proxyState;

        protected override BehaviorSubject<SystemProxyState> Subject { get; }

        public SystemProxyStateControl(IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable,
            IObservable<ProxyState> proxyStateObservableProvider, SystemProxyRegistrationManager systemProxyRegistrationManager)
        {
            _systemProxyRegistrationManager = systemProxyRegistrationManager;
            var current = _systemProxyRegistrationManager.GetSystemProxySetting();

            Subject = new BehaviorSubject<SystemProxyState>(new SystemProxyState(current.BoundHost, current.ListenPort,
                current.Enabled));

            fluxzySettingHolderObservable.Do(t => _fluxzySetting = t.StartupSetting).Subscribe();
            proxyStateObservableProvider.Do(t => _proxyState = t).Subscribe();
        }

        public void On()
        {
            if (_fluxzySetting == null)
                return;

            if (_proxyState == null
                || _proxyState.OnError
                || !_proxyState.BoundConnections.Any())
                return;

            var newSetting = _systemProxyRegistrationManager.Register(_proxyState.BoundConnections.Select(
                p => new IPEndPoint(IPAddress.Parse(p.Address), p.Port)), _fluxzySetting);

            if (newSetting != null)
                Subject.OnNext(new SystemProxyState(newSetting.BoundHost, newSetting.ListenPort, true));
        }

        public void Off()
        {
            _systemProxyRegistrationManager.UnRegister();

            var current = _systemProxyRegistrationManager.GetSystemProxySetting();
            Subject.OnNext(new SystemProxyState(current.BoundHost, current.ListenPort, current.Enabled));
        }
    }
}
