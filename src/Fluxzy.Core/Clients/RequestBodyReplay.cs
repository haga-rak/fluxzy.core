// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;

namespace Fluxzy.Clients
{
    internal sealed class RequestBodyReplay : IDisposable
    {
        private readonly int _limit;
        private byte[]? _buffer;
        private int _length;

        public RequestBodyReplay(int limit)
        {
            _limit = limit;
            _buffer = limit > 0 ? ArrayPool<byte>.Shared.Rent(Math.Min(limit, 16 * 1024)) : null;
        }

        public bool Replayable => _buffer != null;

        public int Length => _length;

        public ReadOnlyMemory<byte> Retained => _buffer == null ? default : new ReadOnlyMemory<byte>(_buffer, 0, _length);

        public void Append(ReadOnlySpan<byte> data)
        {
            if (_buffer == null)
                return;

            if (_length + data.Length > _limit) {
                Release();
                return;
            }

            if (_length + data.Length > _buffer.Length) {
                var grown = ArrayPool<byte>.Shared.Rent(Math.Min(_limit, Math.Max(_buffer.Length * 2, _length + data.Length)));
                _buffer.AsSpan(0, _length).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = grown;
            }

            data.CopyTo(_buffer.AsSpan(_length));
            _length += data.Length;
        }

        private void Release()
        {
            var buffer = _buffer;
            _buffer = null;
            _length = 0;

            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }

        public void Dispose()
        {
            Release();
        }
    }
}
