using Fluxzy;
using Fluxzy.Rules.Actions;

namespace Samples.No014.HookingEvents
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

            await using var proxy = new Proxy(fluxzySetting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                // Do something with the exchange
                // args.Exchange

                Console.WriteLine($"#{args.ExchangeInfo.Id:0000} {args.UpdateType}: " +
                                  $"{args.ExchangeInfo.Method} {args.ExchangeInfo.FullUrl}");
            };

            proxy.Writer.ConnectionUpdated += (_, args) =>
            {
                // Do something with the exchange
                // args.Exchange

                Console.WriteLine($"#{args.Connection.Id:0000} New connection: " +
                                  $"{args.Connection.Authority.HostName} {args.Connection.Authority.Port}");
            };

            proxy.Run();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
