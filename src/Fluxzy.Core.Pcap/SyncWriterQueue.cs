// // Copyright 2022 - Haga Rakotoharivelo
//

using System.Collections.Concurrent;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Core.Pcap.Writing;

namespace Fluxzy.Core.Pcap
{
    internal class SyncWriterQueue : IDisposable
    {
        private ConcurrentDictionary<long, IRawCaptureWriter> _writersByKey = new();
        private ConcurrentDictionary<long, IRawCaptureWriter> _writersBySubId = new();

        private static long _nextSubscriptionId;

        private static readonly string ApplicationName = $"fluxzy {FluxzySharedSetting.RunningVersion} - https://www.fluxzy.io";

        public IRawCaptureWriter GetOrAdd(long key)
        {
            lock (this) {
                if (_writersByKey.TryGetValue(key, out var existing))
                    return existing;

                var writer = CreateWriter(key);
                _writersByKey[key] = writer;
                _writersBySubId[writer.SubscriptionId] = writer;
                return writer;
            }
        }

        public IRawCaptureWriter Rotate(long key)
        {
            lock (this) {
                if (_writersByKey.TryRemove(key, out var stale)) {
                    _writersBySubId.TryRemove(stale.SubscriptionId, out _);

                    try {
                        stale.Dispose();
                    }
                    catch {
                        // ignore stale writer disposal errors
                    }
                }

                var writer = CreateWriter(key);
                _writersByKey[key] = writer;
                _writersBySubId[writer.SubscriptionId] = writer;
                return writer;
            }
        }

        public bool TryGet(long key, out IRawCaptureWriter? writer)
        {
            return _writersByKey.TryGetValue(key, out writer);
        }

        public bool TryRemoveBySubId(long subscriptionId, out IRawCaptureWriter? writer)
        {
            lock (this) {
                if (!_writersBySubId.TryRemove(subscriptionId, out writer))
                    return false;

                if (_writersByKey.TryGetValue(writer!.Key, out var current)
                    && ReferenceEquals(current, writer)) {
                    _writersByKey.TryRemove(writer.Key, out _);
                }

                writer.Flush();
                writer.Dispose();
                return true;
            }
        }

        public void FlushAll()
        {
            foreach (var writer in _writersByKey.Values)
            {
                writer.Flush();
            }
        }

        public void ClearAll()
        {
            var oldWriters = _writersByKey;
            _writersByKey = new ConcurrentDictionary<long, IRawCaptureWriter>();
            _writersBySubId = new ConcurrentDictionary<long, IRawCaptureWriter>();

            foreach (var writer in oldWriters) {
                try {
                    writer.Value.Dispose();
                }
                catch {
                    // We ignore file closing exception
                }
            }

            oldWriters.Clear();
        }

        public void Dispose()
        {
            foreach (var writer in _writersByKey.Values) {
                writer.Dispose();
            }
        }

        private static IRawCaptureWriter CreateWriter(long key)
        {
            var writer = new PcapngWriter(key, ApplicationName) {
                SubscriptionId = Interlocked.Increment(ref _nextSubscriptionId)
            };
            return writer;
        }
    }
}
