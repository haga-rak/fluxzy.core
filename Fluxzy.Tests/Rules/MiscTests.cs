// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class MiscTests
    {
        [Theory]
        [InlineData("-a/", new int[] { 6 })]
        [InlineData("-a/", new int[] { 6, 1024 * 1024 * 9 + 1, 12247})]
        [InlineData("---------------------s4fs6d4fs3df13sf3sdf/", new int[] { 8192, 12247})]
        public void TestMultiPartReader(string boundary, int[] prefferedLength)
        {
            var exampleHeader =
                "Content-Disposition: form-data; name=\"strict-transport-security\"\r\n" +
                "Content-Type: sdfsdf\r\n";

            var fileName = "out.txt";

            try
            {
                var hashes = MultiPartTestCaseBuilder.Write(fileName, boundary, exampleHeader, prefferedLength).ToList();
                var fullPath = new FileInfo(fileName).FullName;

                List<RawMultipartItem> items;

                using (var readStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    items = MultipartReader.ReadItems(readStream, boundary);
                }

                for (var index = 0; index < prefferedLength.Length; index++)
                {
                    var length = prefferedLength[index];
                    var res = items[index];
                    var expected = index + exampleHeader;

                    Assert.Equal(expected, res.RawHeader);
                    Assert.Equal(length, res.Length);

                    using var readStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    using HashedStream stream = new HashedStream(readStream.GetSlicedStream(
                        res.OffSet, res.Length), true);

                    int drainCount = stream.Drain();

                    Assert.Equal(res.Length, drainCount);

                    var expectedHash = hashes[index];
                    var resultHash = Convert.ToBase64String(stream.Compute() ?? Array.Empty<byte>());

                    Assert.Equal(expectedHash, resultHash);
                }
            }
            finally
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }

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

            return inputContent.HashBae ?? string.Empty;
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