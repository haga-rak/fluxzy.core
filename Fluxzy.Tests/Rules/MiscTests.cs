// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class MiscTests
    {
        [Theory]
        [InlineData("-a/", new int[] { 6 })]
        [InlineData("-a/", new int[] { 6, 24 })]
        public async Task TestMultiPartReader(string boundary, int[] preferedLength)
        {
            var exampleHeader =
                "Content-Disposition: form-data; name=\"strict-transport-security\"\r\n" +
                "Content-Type: sdfsdf\r\n";

            var fileName = "out.txt";

            var hashes = MultiPartTestCaseBuilder.Write(fileName, boundary, exampleHeader, preferedLength).ToList();
            var fullPath = new FileInfo(fileName).FullName;

            using var readStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var items = await MultipartReader.ReadItems(readStream, boundary);


        }
    }

    public class MultiPartTestCaseBuilder
    {
        public static string WriteContent(Stream output, string header, RandomDataStream inputContent, string boundary)
        {
            var fullHeader = "--" + boundary + "\r\n" + header + "\r\n";

            output.Write(Encoding.UTF8.GetBytes(fullHeader));

            inputContent.CopyTo(output);

            output.Write(Encoding.UTF8.GetBytes("\r\n"));

            return inputContent.Hash ?? string.Empty;
        }

        public static IEnumerable<string> Write(string fileName, string boundary, string exampleHeader,   int [] preferedLengths)
        {
            Random r = new Random(9);

            using var outStream = File.Create(fileName);

            for (var index = 0; index < preferedLengths.Length; index++)
            {
                var length = preferedLengths[index];
                using RandomDataStream randomDataStream = new RandomDataStream(r.Next(), length, true);

                yield return WriteContent(outStream, index + "" +exampleHeader, randomDataStream, boundary);
            }

            outStream.Write(Encoding.ASCII.GetBytes("--" + boundary));
            outStream.Write(Encoding.ASCII.GetBytes("--\r\n"));

        }
    }
}