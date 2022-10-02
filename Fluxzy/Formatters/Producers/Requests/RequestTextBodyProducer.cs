// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class RequestTextBodyProducer : IFormattingProducer<RequestTextBodyResult>
    {
        public string ResultTitle => "Text content";

        public RequestTextBodyResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.RequestBodyText == null)
            {
                return null;
            }

            return new RequestTextBodyResult(ResultTitle, context.RequestBodyText);
        }
    }

    public class RequestTextBodyResult : FormattingResult
    {
        public string Text { get; }

        public RequestTextBodyResult(string title, string text) : base(title)
        {
            Text = text;
        }
    }
}