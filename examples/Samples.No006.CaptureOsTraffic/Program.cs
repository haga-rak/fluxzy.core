using Fluxzy;
using Fluxzy.Core;

namespace Samples.No006.CaptureOsTraffic
{
    internal class Program
    {
        /// <summary>
        ///  Registering as system proxy
        /// </summary>
        static async Task Main()
        {
            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting.CreateLocalRandomPort();

            // Create a proxy instance
            await using var proxy = new Proxy(fluxzyStartupSetting);

            var endpoints = proxy.Run();

            await using var proxyRegistration = await SystemProxyRegistrationHelper.Create(endpoints.First());
            
            // Fluxzy is now registered as the system proxy, the proxy will revert
            // back to the original settings when proxyRegistration is disposed.

            Console.WriteLine("Press any key to halt proxy and unregistered"); 

            Console.ReadKey();
        }
    }
}