// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
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

        /// <summary>
        ///   Html tag name after which the injection will be performed
        /// </summary>
        [ActionDistinctive(Description = "Html tag name after which the injection will be performed")]
        public string Tag { get; set; } 

        /// <summary>
        ///  The text to be injected
        /// </summary>
        [ActionDistinctive(Description = "The text to be injected")]
        public string? Text { get; set; }

        /// <summary>
        /// If true, the text will be read from a file
        /// </summary>
        [ActionDistinctive(Description = "If true, the text will be read from a file")]
        public bool FromFile { get; set; } = false; 
        
        /// <summary>
        /// If FromFile is true, the file name to read from
        /// </summary>
        [ActionDistinctive(Description = "If FromFile is true, the file name to read from")]
        public string ? FileName { get; set; }

        /// <summary>
        ///  Encoding IANA name, if not specified, UTF8 will be used
        /// </summary>
        [ActionDistinctive(Description = "IANA name encoding", DefaultValue = "utf8")]
        public string? Encoding { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ActionDistinctive(Description = "Restrict substitution to text/html response", DefaultValue = "true")]
        public bool RestrictToHtml { get; set; } = true;

        public override FilterScope ActionScope { get; } = FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription { get; } = "inject tag"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null || exchange.Id == 0)
                return default;

            if (!FromFile && string.IsNullOrEmpty(Text)) {
                throw new RuleExecutionFailureException("Text is null or empty");
            }
            
            if (FromFile && string.IsNullOrEmpty(FileName)) {
                throw new RuleExecutionFailureException("FileName is null or empty");
            }

            if (RestrictToHtml) {
                var isHtml = exchange.GetResponseHeaders()?
                                          .Any(r =>
                                              r.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase)
                                              && r.Value.Span.Contains("text/html", StringComparison.Ordinal))
                    ?? false;

                if (!isHtml)
                    return default;
            }

            var encoding = string.IsNullOrEmpty(Encoding) ? System.Text.Encoding.UTF8
                : System.Text.Encoding.GetEncoding(Encoding);
            
            var stream = !FromFile ? (Stream) new MemoryStream(encoding.GetBytes(Text!))
                : new FileStream(FileName!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                context.RegisterResponseBodySubstitution(
                    new InjectAfterHtmlTagSubstitution(encoding, Tag, stream));

            return default;
        }
    }

    internal class InjectAfterHtmlTagSubstitution : IStreamSubstitution
    {
        private readonly Encoding _encoding;
        private readonly Stream _injectedStream;
        private readonly byte[] _matchingPattern;

        public InjectAfterHtmlTagSubstitution(Encoding encoding, string htmlTag, 
            Stream injectedStream)
        {
            _encoding = encoding;
            _injectedStream = injectedStream;
            _matchingPattern = _encoding.GetBytes(htmlTag); 
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            var stream = new InjectStreamOnStream(originalStream,
                new SimpleHtmlTagOpeningMatcher(_encoding, StringComparison.OrdinalIgnoreCase,
                    false), _matchingPattern, _injectedStream);

            return new ValueTask<Stream>(stream); 
        }
    }
}
