using Fluxzy;
using Fluxzy.Rules.Actions.HighLevelActions;

namespace Samples.No012.AddResponseCookie
{
    internal class Program
    {
        /// <summary>
        ///  Add response cookie on any responses 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.ConfigureRule()
                         // Add response cookie on any ongoing response
                         .WhenAny()
                         .Do(new SetResponseCookieAction("fluxzy-response-cookie", "sweet")
                         {
                             Path = "/",
                             ExpireInSeconds = 3600, 
                             HttpOnly = true,
                             Secure = true,
                             SameSite = "Lax",
                             MaxAge = 3600,
                             // Domain =  -- set domain here 
                         });

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Run();

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }
    
    }
}
