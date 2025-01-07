using Fluxzy;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;

namespace Samples.No016.ImpersonateBrowser
{
    internal class Program
    {
        /// <summary>
        ///  This example shows how to impersonate Chrome 131's fingerprint
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {
            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting.CreateLocalRandomPort();

            // Mandatory, BouncyCastle must be used to reproduce the fingerprints
            fluxzyStartupSetting.UseBouncyCastleSslEngine();

            // Add an impersonation rule for Chrome 131
            fluxzyStartupSetting.AddAlterationRulesForAny(
                new ImpersonateAction(ImpersonateProfileManager.Chrome131Windows));

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
