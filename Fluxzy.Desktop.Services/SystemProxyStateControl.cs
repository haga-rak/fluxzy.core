using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class SystemProxyStateControl : ObservableProvider<SystemProxyState>
    {
        private FluxzySetting?  _fluxzySetting;

        public SystemProxyStateControl(IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable)
        {
            var current = SystemProxyRegistration.GetSystemProxySetting();
            Subject = new BehaviorSubject<SystemProxyState>(new SystemProxyState(current.BoundHost, current.ListenPort, current.Enabled));

            fluxzySettingHolderObservable.Do(t => this._fluxzySetting = t.StartupSetting).Subscribe(); 
        }

        protected override BehaviorSubject<SystemProxyState> Subject { get; }

        public void On()
        {
            if (_fluxzySetting == null)
                return;

            var newSetting = SystemProxyRegistration.Register(_fluxzySetting);

            if (newSetting != null)
                Subject.OnNext(new SystemProxyState(newSetting.BoundHost, newSetting.ListenPort, true));
        }

        public void Off()
        {
            SystemProxyRegistration.UnRegister();

            var current = SystemProxyRegistration.GetSystemProxySetting();
            Subject.OnNext(new SystemProxyState(current.BoundHost, current.ListenPort, current.Enabled));
        }

    }
}