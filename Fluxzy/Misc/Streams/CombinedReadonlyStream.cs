﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    internal class CombinedReadonlyStream : Stream
    {
        private long _position;
        private readonly bool _closeStreams;
        private IEnumerator<Stream> _iterator;
        private Stream? _current;

        public CombinedReadonlyStream(bool closeStreams, params Stream[] args)
        : this(args, closeStreams)
        {
        }

        public CombinedReadonlyStream(IEnumerable<Stream> source, bool closeStreams = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _iterator = source.GetEnumerator();
            _closeStreams = closeStreams;
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        private void EndOfStream()
        {
            if (_closeStreams && _current != null)
            {
                _current.Close();
                _current.Dispose();
            }

            _current = null;
        }

        private Stream Current
        {
            get
            {
                if (_current != null) return _current;
                if (_iterator == null) throw new ObjectDisposedException(GetType().Name);
                if (_iterator.MoveNext())
                {
                    _current = _iterator.Current;
                }
                return _current;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                EndOfStream();
                _iterator.Dispose();
                _iterator = null;
                _current = null;
            }

            base.Dispose(disposing);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        public override bool CanSeek => false;

        public override bool CanTimeout => false;

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {

        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set { if (value != _position) throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = 0;
            while (count > 0)
            {
                Stream stream = Current;

                if (stream == null)
                    break;

                int thisCount = stream.Read(buffer, offset, count);
                result += thisCount;
                count -= thisCount;
                offset += thisCount;
                if (thisCount == 0) EndOfStream();
            }
            _position += result;
            return result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            int result = 0;

            while (count > 0)
            {
                Stream stream = Current;

                if (stream == null)
                    break;

                int currentReadCount =
                    stream is MemoryStream ?
                        stream.Read(buffer, offset, count) :
                        await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);

                result += currentReadCount;
                count -= currentReadCount;
                offset += currentReadCount;

                if (currentReadCount == 0)
                {
                    EndOfStream();
                    // break;
                }
                else
                {
                    break; // We already have something, + NetworkStream may be blocked forever
                }
            }

            _position += result;

            return result;
        }
    }
}