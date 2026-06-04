// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     Reproduction for "InjectHtmlTagAction can't inject" reported against a page whose markup
    ///     starts with an attributed root tag (`&lt;html lang="fr"&gt;`) and whose `&lt;head&gt;` is
    ///     immediately followed by a very large inline `&lt;script&gt;` block.
    /// </summary>
    public class InjectHtmlHeadReproTests
    {
        // Faithful reproduction of the reported structure:
        //  - <html> carries an attribute (lang="fr")
        //  - a newline separates <html ...> from <head>
        //  - <head> is immediately followed by a large inline <script> block containing quotes/urls/<>
        private const string ProblemHtml =
            "<html lang=\"fr\">\n" +
            "<head><script>\n" +
            "            var frzScriptsToPreload = document.createDocumentFragment();\n" +
            "            var frzScriptsToPreloadUrls = [\"https://www.promovacances.com/resources/static/dist/obflnk/obflnk.js?r=a41433c3663743978b0208b64ce78536\",\"//widget.trustpilot.com/bootstrap/v5/tp.widget.bootstrap.min.js\"];\n" +
            "            var frzScriptsToPreloadScripts = [{\"src\":\"https://www.promovacances.com/resources/static/dist/vendor/vendor.js\",\"module\":false}];\n" +
            "        </script></head>\n" +
            "<body>hello</body></html>";

        private const string Injected = "<!--INJECTED-->";

        [Fact]
        public void Matcher_should_find_head_in_problem_html()
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase, false);

            var (index, length) = matcher.FindIndex(ProblemHtml, "head");

            var expectedIndex = ProblemHtml.IndexOf("<head", StringComparison.Ordinal);

            Assert.Equal(expectedIndex, index);
            Assert.Equal("<head>".Length, length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(23)]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(516)]
        [InlineData(1024)]
        [InlineData(8192)]
        public void Should_inject_after_head_sync(int bufferSize)
        {
            var result = Inject(ProblemHtml, "head", Injected, bufferSize);

            var expected = ProblemHtml.Replace("<head>", "<head>" + Injected);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(23)]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(516)]
        [InlineData(1024)]
        [InlineData(8192)]
        public async Task Should_inject_after_head_async(int bufferSize)
        {
            var result = await InjectAsync(ProblemHtml, "head", Injected, bufferSize);

            var expected = ProblemHtml.Replace("<head>", "<head>" + Injected);

            Assert.Equal(expected, result);
        }

        private static InjectStreamOnStream BuildStream(string content, string pattern, string inserted)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase, false);

            return new InjectStreamOnStream(
                new MemoryStream(Encoding.UTF8.GetBytes(content)),
                matcher,
                Encoding.UTF8.GetBytes(pattern),
                new MemoryStream(Encoding.UTF8.GetBytes(inserted)));
        }

        private static string Inject(string content, string pattern, string inserted, int bufferSize)
        {
            using var stream = BuildStream(content, pattern, inserted);

            return stream.ReadToEndWithCustomBuffer(bufferSize: bufferSize);
        }

        private static async Task<string> InjectAsync(string content, string pattern, string inserted, int bufferSize)
        {
            using var stream = BuildStream(content, pattern, inserted);

            return await stream.ReadToEndWithCustomBufferAsync(bufferSize: bufferSize);
        }
    }
}
