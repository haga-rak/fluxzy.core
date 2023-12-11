using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;

namespace Fluxzy.Tests.UnitTests.Substitutions.Actions
{
    /// <summary>
    /// A simple action that install a body substitution
    /// </summary>
    internal class AddResponseBodyStreamSubstitutionAction : Action
    {
        private readonly IStreamSubstitution _substitution;

        public AddResponseBodyStreamSubstitutionAction(IStreamSubstitution substitution)
        {
            _substitution = substitution;
        }

        public override FilterScope ActionScope { get; } = FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription { get; } = "Test action";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ResponseBodySubstitution = _substitution;
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

        public async ValueTask<Stream> Substitute(Stream originalStream)
        {
            var length = await originalStream.DrainAsync();
            return new MemoryStream(Encoding.UTF8.GetBytes(_content));
        }
    }
}
