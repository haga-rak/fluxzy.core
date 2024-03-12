using Fluxzy;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No009.InjectCodeSnippet
{
    internal class Program
    {
        /// <summary>
        ///  Inject a code snippet into traversing html content
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()
                         // When the content type is text/html
                         .When(new RequestHeaderFilter("text/html", "content-type"))

                         // Inject the following html tag into the head tag
                         .Do(new InjectHtmlTagAction()
                         {
                             HtmlContent = "<style>body { background-color: red !important; }</style>",
                             Tag = "head", // we insert on tag header to execute the snippet early
                         });

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    }
}
