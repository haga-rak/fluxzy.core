using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Writers
{
    public class EventOnlyArchiveWriter : RealtimeArchiveWriter
    {
        public override void UpdateTags(IEnumerable<Tag> tags)
        {

        }

        public override ValueTask Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            return default;
        }

        public override ValueTask Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            return default;
        }

        public override Stream CreateRequestBodyStream(int exchangeId)
        {
            return new MockedWriteStream();
        }

        public override Stream CreateResponseBodyStream(int exchangeId)
        {
            return new MockedWriteStream();
        }
    }
}