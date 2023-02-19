using Fluxzy.Core.Proxy;

namespace Fluxzy.NativeOps.SystemProxySetup.Linux
{
    internal class LinuxProxySetter : ISystemProxySetter
    {
        private EnvProxySetter _internalSetter = new EnvProxySetter(); 
        public void ApplySetting(SystemProxySetting value)
        {
            _internalSetter.ApplySetting(value);
            
            // DO here proxy configuration Distribution related 
        }

        public SystemProxySetting ReadSetting()
        {
            // DO here proxy configuration Distribution related 
            
            return _internalSetter.ReadSetting(); 
        }
    }
}