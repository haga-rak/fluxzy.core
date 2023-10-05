using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;
using Xunit;

namespace Fluxzy.Tests.Cli.Dissects
{
    public class SequentialFormatterTests
    {
        private readonly Dictionary<string, IDissectionFormatter<int>> _formatterMap 
            = new Dictionary<string, string>() {
                ["hello"] = "world",
                ["foo"] = "bar",
                ["baz"] = "qux"
                }
                .ToDictionary(
                    t => t.Key, 
                    t => (IDissectionFormatter<int>) new MockFormatter(t.Key, t.Value),
                    StringComparer.OrdinalIgnoreCase);

        [Theory]
        [InlineData("{hello}", "world")]
        [InlineData("{hello} {hello}", "world world")]
        [InlineData("{hello} {Hello}", "world world")]
        [InlineData("{hel", "{hel")]
        [InlineData("{hel{hello}", "{helworld")]
        public async Task Verify(string format, string expected)
        {
            var sequentialFormatter = new SequentialFormatter();

            using var stdoutStream = new MemoryStream();
            using var writer = new StreamWriter(stdoutStream);

            await sequentialFormatter.Format(format, _formatterMap, writer, StreamWriter.Null, 0);
            await writer.FlushAsync();

            var actual = Encoding.UTF8.GetString(stdoutStream.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}
