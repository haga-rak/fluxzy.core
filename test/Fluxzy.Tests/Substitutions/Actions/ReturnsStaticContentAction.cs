using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;

namespace Fluxzy.Tests.Substitutions.Actions
{
    /// <summary>
    /// A simple action that returns the content length of the original response instead of the response
    /// </summary>
    internal class ReturnsStaticContentAction : Action
    {
        private readonly string _content;

        public ReturnsStaticContentAction(string content)
        {
            _content = content;
        }

        public override FilterScope ActionScope { get; } = FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription { get; } = "Test action";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ResponseBodySubstitution = new ReturnsContentLengthSubstitution(_content);
            return default;
        }
    }

    internal class ReturnsContentLengthSubstitution : IStreamSubstitution
    {
        private readonly string _content;

        public ReturnsContentLengthSubstitution(string content)
        {
            _content = content;
        }

        public async ValueTask<Stream> Substitute(Stream stream)
        {
            var length = await stream.DrainAsync();
            return new MemoryStream(Encoding.UTF8.GetBytes(_content));
        }
    }
}
