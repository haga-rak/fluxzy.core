using System.Collections.Generic;
using System.IO;
using System.Threading;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Writers
{
    public class EventOnlyArchiveWriter : RealtimeArchiveWriter
    {
        public override void UpdateTags(IEnumerable<Tag> tags)
        {

        }

        public override void Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
        }

        public override void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
        }

        public override Stream CreateRequestBodyStream(int exchangeId)
        {
            return new EmptyWriteStream();
        }

        public override Stream CreateResponseBodyStream(int exchangeId)
        {
            return new EmptyWriteStream();
        }

        public override Stream CreateWebSocketRequestContent(int exchangeId, int messageId)
        {
            return new EmptyWriteStream();
        }

        public override Stream CreateWebSocketResponseContent(int exchangeId, int messageId)
        {
            return new EmptyWriteStream();
        }
    }
}