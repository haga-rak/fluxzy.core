using System.Diagnostics;

namespace Fluxzy.Rules.Actions
{
    internal class DefaultInstantProvider : IInstantProvider
    {
        public long ElapsedMillis => Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000);
    }
}