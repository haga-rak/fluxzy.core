using Fluxzy;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No008.ForcingHttpVersion
{
    internal class Program
    {
        /// <summary>
        ///  Force the client to use HTTP/1.1 or HTTP/2
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()
                         .WhenHostMatch("must-be-http-11.com")
                         .Do(new ForceHttp11Action())
                         .WhenHostMatch("must-be-http-2.com")
                         .Do(new ForceHttp11Action());

            // Create a new proxy instance 
            await using var proxy = new Proxy(fluxzySetting);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
