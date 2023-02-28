// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Collections.Concurrent;

namespace Fluxzy.Interop.Pcap
{
    internal class SyncWriterQueue : IDisposable
    {
        private ConcurrentDictionary<long, CustomCaptureWriter> _writers = new();

        public CustomCaptureWriter GetOrAdd(long key)
        {
            return _writers.GetOrAdd(key, (k) => new CustomCaptureWriter(k));;
        }

        public bool TryGet(long key, out CustomCaptureWriter? writer)
        {
            return _writers.TryGetValue(key, out writer);
        }

        public bool TryRemove(long key, out CustomCaptureWriter? writer)
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
            _writers = new ConcurrentDictionary<long, CustomCaptureWriter>();

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