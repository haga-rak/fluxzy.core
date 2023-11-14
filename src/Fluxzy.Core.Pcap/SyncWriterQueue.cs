// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Collections.Concurrent;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Core.Pcap.Writing;

namespace Fluxzy.Core.Pcap
{
    internal class SyncWriterQueue : IDisposable
    {
        private ConcurrentDictionary<long, IRawCaptureWriter> _writers = new();

        public IRawCaptureWriter GetOrAdd(long key)
        {
            lock (this) {
                return _writers.GetOrAdd(key,
                    (k) => new PcapngWriter(k, "fluxzy v0.15.9 - https://www.fluxzy.io")); ;
            }
        }

        public bool TryGet(long key, out IRawCaptureWriter? writer)
        {
            return _writers.TryGetValue(key, out writer);
        }

        public bool TryRemove(long key, out IRawCaptureWriter? writer)
        {
            return _writers.TryRemove(key, out writer);
        }

        public void FlushAll()
        {
            foreach (var writer in _writers.Values)
            {
                writer.Flush();
            }
        }

        public void ClearAll()
        {
            var oldWriter = _writers;
            _writers = new ConcurrentDictionary<long, IRawCaptureWriter>();

            foreach (var writer in oldWriter) {
                try {
                    writer.Value.Dispose();
                }
                catch {
                    // We ignore file closing exception 
                }
            }

            oldWriter.Clear();
        }

        public void Dispose()
        {
            foreach (var writer in _writers.Values) {
                writer.Dispose();
            }
        }
    }
}