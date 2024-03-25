using Fluxzy;
using Fluxzy.Certificates;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No007.UsingClientCertificate
{
    internal class Program
    {
        /// <summary>
        ///  Use mTLS to authenticate the client
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            // Load from a PKCS12 file (pfx and p12)
            var certificate = Certificate.LoadFromPkcs12("clientCertificiate.p12", "password");

            // Load from user store with serial number
            // var certificate = Certificate.LoadFromUserStoreBySerialNumber("xxxx")

            // Load from user store with thumbprint
            // var certificate = Certificate.LoadFromUserStoreByThumbprint("xxxxx");

            fluxzySetting.ConfigureRule()
                         .WhenHostEndsWith("mtls-mandatory.com")
                         .Do(new SetClientCertificateAction(certificate)); 

            // Create a new proxy instance 
            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
