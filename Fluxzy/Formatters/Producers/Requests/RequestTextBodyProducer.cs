// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Formatters.Producers.Requests
{
    public class RequestTextBodyProducer : IFormattingProducer<RequestTextBodyResult>
    {
        public string ResultTitle => "Text content";

        public RequestTextBodyResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.RequestBodyText == null)
                return null;

            if (string.IsNullOrEmpty(context.RequestBodyText))
                return null; 

            return new RequestTextBodyResult(ResultTitle, context.RequestBodyText);
        }
    }

    public class RequestTextBodyResult : FormattingResult
    {
        public RequestTextBodyResult(string title, string text)
            : base(title)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
