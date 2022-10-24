﻿namespace Fluxzy.Formatters.Producers.Responses
{
    public class ResponseTextContentProducer : IFormattingProducer<ResponseTextContentResult>
    {
        public string ResultTitle => "Text content";

        public ResponseTextContentResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.IsTextContent && !string.IsNullOrWhiteSpace(context.RequestBodyText))
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