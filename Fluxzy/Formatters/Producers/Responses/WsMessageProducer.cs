// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Clients.H11;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class WsMessageProducer : IFormattingProducer<WsMessageFormattingResult>
    {
        public string ResultTitle => "Websocket messages"; 

        public WsMessageFormattingResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (exchangeInfo.WebSocketMessages == null || !exchangeInfo.WebSocketMessages.Any())
            {
                return null; 
            }

            return new WsMessageFormattingResult(ResultTitle, exchangeInfo.WebSocketMessages.ToList());
        }
    }

    public class WsMessageFormattingResult : FormattingResult
    {
        public List<WsMessage> Messages { get; }

        public WsMessageFormattingResult(string title, List<WsMessage> messages)
            : base(title)
        {
            Messages = messages;
        }
    }
}