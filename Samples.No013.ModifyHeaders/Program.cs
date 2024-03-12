using Fluxzy;
using Fluxzy.Rules.Actions;

namespace Samples.No013.ModifyHeaders
{
    internal class Program
    {
        /// <summary>
        /// Manipulating request and response headers
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()

                         // Add response cookie on any ongoing response
                         .WhenAny()
                         .Do(
                             new AddRequestHeaderAction("new-header", "value"),
                             new DeleteRequestHeaderAction("Date"), // Delete request header
                             new UpdateRequestHeaderAction("User-Agent", "{{previous}} - add suffix to user-agent"),
                             new AddResponseHeaderAction("new-response-header", "value"),
                             new DeleteResponseHeaderAction("Date"), // Delete response header
                             new UpdateResponseHeaderAction("Server", "{{previous}} - add suffix to server"));

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }
}
