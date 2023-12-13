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
    ///     This action analyze a response body and inject a string after the first specified html tag.
    ///     This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection.
    ///     This action is issued essentially to inject a script tag in a html page.
    /// </summary>
    [ActionMetadata(
        "This action analyze a  response body and inject a string after the first a specified html tag. " +
        "This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection. " +
        "This action is issued essentially to inject a script tag in a html page.",
        NonDesktopAction = true)]
    public class InjectAfterHtmlTagAction : Action
    {
        public InjectAfterHtmlTagAction(string htmlTag, string injection)
        {
            HtmlTag = htmlTag;
            Injection = injection;
        }

        [ActionDistinctive(Description = "Html tag name after which the injection will be performed")]
        public string HtmlTag { get; set; }

        [ActionDistinctive(Description = "String to inject")]
        public string Injection { get; set; }

        /// <summary>
        ///  Encoding IANA name, if not specified, UTF8 will be used
        /// </summary>
        [ActionDistinctive(Description = "Encoding")]
        public string? Encoding { get; set; }

        public override FilterScope ActionScope { get; } = FilterScope.OnAuthorityReceived;

        public override string DefaultDescription { get; } = "inject tag"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null || exchange.Id == 0)
                return default;

            var encoding = string.IsNullOrEmpty(Encoding) ? System.Text.Encoding.UTF8
                : System.Text.Encoding.GetEncoding(Encoding);

            if (context.ResponseBodySubstitution == null)
                context.ResponseBodySubstitution = new ResponseBodySubstitution();

            context.ResponseBodySubstitution.Add(new ResponseBodySubstitution.Substitution
            {
                HtmlTag = HtmlTag,
                Injection = Injection
            });

            return default;
        }
    }

    public class InjectAfterHtmlTagSubstitution : IStreamSubstitution
    {
        private readonly Encoding _encoding;

        public InjectAfterHtmlTagSubstitution(Encoding encoding)
        {
            _encoding = encoding;
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            Encoding.UTF8.WebName

            var stream = new InjectStreamOnStream(originalStream, 
                new SimpleHtmlTagOpeningMatcher(_encoding, StringComparison.OrdinalIgnoreCase,
                    false))

            return default; 
        }
    }
}
