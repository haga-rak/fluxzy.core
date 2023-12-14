// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     This action analyze a response body and inject a text after the first specified html tag.
    ///     This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection.
    ///     This action is issued essentially to inject a script tag in a html page.
    /// </summary>
    [ActionMetadata(
        "This action analyze a  response body and inject a text after the first a specified html tag. " +
        "This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection. " +
        "This action is issued essentially to inject a script tag in a html page.",
        NonDesktopAction = true)]
    public class InjectAfterHtmlTagAction : Action
    {
        public InjectAfterHtmlTagAction(string tag, string text)
        {
            Tag = tag;
            Text = text;
        }

        [ActionDistinctive(Description = "Html tag name after which the injection will be performed")]
        public string Tag { get; set; } 

        [ActionDistinctive(Description = "The text to be injected")]
        public string Text { get; set; }

        /// <summary>
        ///  Encoding IANA name, if not specified, UTF8 will be used
        /// </summary>
        [ActionDistinctive(Description = "Encoding")]
        public string? Encoding { get; set; }

        public override FilterScope ActionScope { get; } = FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription { get; } = "inject tag"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null || exchange.Id == 0)
                return default;

            var encoding = string.IsNullOrEmpty(Encoding) ? System.Text.Encoding.UTF8
                : System.Text.Encoding.GetEncoding(Encoding);

                context.RegisterResponseBodySubstitution(
                    new InjectAfterHtmlTagSubstitution(encoding, Tag, Text));

            return default;
        }
    }

    internal class InjectAfterHtmlTagSubstitution : IStreamSubstitution
    {
        private readonly Encoding _encoding;
        private readonly byte[] _matchingPattern;
        private readonly byte[] _binaryText;

        public InjectAfterHtmlTagSubstitution(Encoding encoding, string htmlTag, string text)
        {
            _encoding = encoding;
            _matchingPattern = _encoding.GetBytes(htmlTag); 
            _binaryText = _encoding.GetBytes(text);
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            var memoryStream = new MemoryStream(_binaryText);

            var stream = new InjectStreamOnStream(originalStream,
                new SimpleHtmlTagOpeningMatcher(_encoding, StringComparison.OrdinalIgnoreCase,
                    false), _matchingPattern, memoryStream);

            return new ValueTask<Stream>(stream); 
        }
    }
}
