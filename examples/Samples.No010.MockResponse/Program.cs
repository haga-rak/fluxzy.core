using Fluxzy;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No010.MockResponse
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()
                         // Mock with an immediate response
                         .WhenHostMatch("www.google.com", StringSelectorOperation.StartsWith)
                         .Do(new MockedResponseAction(
                             MockedResponseContent.CreateFromPlainText("This is a plain text content")));

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }
}
