// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H11;
using Fluxzy.Misc;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class WsMessageProducer : IFormattingProducer<WsMessageFormattingResult>
    {
        public string ResultTitle => "Websocket messages";

        public WsMessageFormattingResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (exchangeInfo.WebSocketMessages == null || !exchangeInfo.WebSocketMessages.Any())
                return null;

            foreach (var message in exchangeInfo.WebSocketMessages
                                                .Where(d => d.Data != null)) {
                if (!ArrayTextUtilities.IsText(message.Data))
                    continue;

                message.DataString = Encoding.UTF8.GetString(message.Data!);
            }

            return new WsMessageFormattingResult(ResultTitle, exchangeInfo.WebSocketMessages.ToList());
        }
    }

    public class WsMessageFormattingResult : FormattingResult
    {
        public WsMessageFormattingResult(string title, List<WsMessage> messages)
            : base(title)
        {
            Messages = messages;
        }

        public List<WsMessage> Messages { get; }
    }
}
