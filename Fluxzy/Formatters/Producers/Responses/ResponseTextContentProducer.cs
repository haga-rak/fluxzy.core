// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Formatters.Producers.Responses
{
    public class ResponseTextContentProducer : IFormattingProducer<ResponseTextContentResult>
    {
        public string ResultTitle => "Text content";

        public ResponseTextContentResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.IsTextContent && !string.IsNullOrWhiteSpace(context.ResponseBodyText))
                return new ResponseTextContentResult(ResultTitle);

            return null;
        }
    }

    public class ResponseTextContentResult : FormattingResult
    {
        public ResponseTextContentResult(string title)
            : base(title)
        {
        }
    }
}
