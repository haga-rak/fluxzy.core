using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Samples.No015.ReadingArchive
{
    internal class Program
    {
        /// <summary>
        ///  This example shows how to read an archive file
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var archiveReader = new FluxzyArchiveReader("example.com.fxzy");

            // Use DirectoryArchiveReader to read from a dump directory 
            // var archiveReader = new DirectoryArchiveReader("directory_name")

            var allExchanges = archiveReader.ReadAllExchanges(); 

            var exchange = allExchanges.First();

            foreach (var header in exchange.GetRequestHeaders()) {
                Console.WriteLine($"{header.Name}: {header.Value}");
            }

            Console.WriteLine();

            foreach (var header in exchange.GetResponseHeaders()!) {
                Console.WriteLine($"{header.Name}: {header.Value}");
            }

            var responseBodyStream = archiveReader.GetDecodedResponseBody(exchange.Id)!;

            var responseAsString = responseBodyStream.ReadToEndGreedy();

            Console.WriteLine(responseAsString);
        }
    }
}
