using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving.Writers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Writers
{
    public class EventOnlyArchiveWriter : RealtimeArchiveWriter
    {
        public override Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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