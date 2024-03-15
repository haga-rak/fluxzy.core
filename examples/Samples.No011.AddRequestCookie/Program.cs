using Fluxzy;
using Fluxzy.Rules.Actions.HighLevelActions;

namespace Samples.No011.AddRequestCookie
{
    internal class Program
    {
        /// <summary>
        ///  Add request cookie on any ongoing requests
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()
                         // Add request cookie on any ongoing request
                         .WhenAny()
                         .Do(new SetRequestCookieAction("fluxzy-cookie", "yes"));

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }
}
