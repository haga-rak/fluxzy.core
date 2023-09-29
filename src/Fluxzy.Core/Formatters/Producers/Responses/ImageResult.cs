// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Extensions;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class ImageResultProducer : IFormattingProducer<ImageResult>
    {
        public string ResultTitle => "Image";

        public ImageResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (exchangeInfo.ContentType != "img")
                return null; 

            if (context.ResponseBodyLength == null)
                return null;

            return new ImageResult(ResultTitle, exchangeInfo.GetResponseHeaderValue("content-type")!); 
        }
    }


    public class ImageResult : FormattingResult
    {
        public string ContentType { get; }

        public ImageResult(string title, string contentType)
            : base(title)
        {
            ContentType = contentType;
        }
    }
}
