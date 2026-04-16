// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Fluxzy.Core
{
    public class ExchangeScope : IDisposable
    {
        private char[]? _first;
        private char[]? _second;
        private List<char[]>? _overflow;

        private bool _disposed;

        public Memory<char> RegisterForReturn(int length)
        {
            var array = ArrayPool<char>.Shared.Rent(length);

            lock (this) {
                if (_first == null) {
                    _first = array;
                }
                else if (_second == null) {
                    _second = array;
                }
                else {
                    (_overflow ??= new List<char[]>()).Add(array);
                }
            }

            return new Memory<char>(array, 0, length);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_first != null) {
                ArrayPool<char>.Shared.Return(_first);
                _first = null;
            }

            if (_second != null) {
                ArrayPool<char>.Shared.Return(_second);
                _second = null;
            }

            if (_overflow != null) {
                foreach (var array in _overflow) {
                    ArrayPool<char>.Shared.Return(array);
                }

                _overflow = null;
            }
        }
    }
}
