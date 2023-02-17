// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Collections.Concurrent;

namespace Fluxzy.Interop.Pcap
{
    internal class SyncWriterQueue : IDisposable
    {
        private readonly ConcurrentDictionary<long, CustomCaptureWriter> _writers = new();

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


        public void Dispose()
        {
            foreach (var writer in _writers.Values) {
                writer.Dispose();
            }
        }
    }
}