using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Rules.Actions
{
    internal class AverageThrottleSubstitution : IStreamSubstitution
    {
        private readonly AverageThrottler _averageThrottler;

        public AverageThrottleSubstitution(AverageThrottler averageThrottler)
        {
            _averageThrottler = averageThrottler;
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            return new ValueTask<Stream>(new BufferedThrottleStream(originalStream, _averageThrottler));
        }
    }
}