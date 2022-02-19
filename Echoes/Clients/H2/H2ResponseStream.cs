// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.Helpers;

namespace Echoes.H2
{
    //public class H2ResponseStream : Stream
    //{
    //    private readonly MemoryPool<byte> _memoryPool;
    //    private readonly H2Message _owner;
    //    private bool _done = false;
    //    private int _offset = 0; 

    //    private readonly Channel<MutableMemoryOwner<byte>> _resultChannel = Channel.CreateUnbounded<MutableMemoryOwner<byte>>(new UnboundedChannelOptions()
    //    {
    //        SingleWriter = true,
    //        SingleReader = true,
    //        AllowSynchronousContinuations = true
    //    });

    //    internal H2ResponseStream(MemoryPool<byte> memoryPool, H2Message owner)
    //    {
    //        _memoryPool = memoryPool;
    //        _owner = owner;
    //    }

    //    public override void Flush()
    //    {

    //    }

    //    internal void Feed(ReadOnlyMemory<byte> data, bool end)
    //    {
    //        var memoryOwner = _memoryPool.RendExact(data.Length);

    //        data.CopyTo(memoryOwner.Memory);
    //        _resultChannel.Writer.TryWrite(memoryOwner);

    //        if (end)
    //            _resultChannel.Writer.TryComplete();
    //    }

    //    private MutableMemoryOwner<byte> _remaining = null;

    //    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    //    {
    //        var readenSize =  await InternalReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    //        _owner.OnDataConsumedByCaller(readenSize);
    //        return readenSize; 
    //    }

    //    private async Task<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    //    {
    //        if (_remaining is { Memory: { IsEmpty: false } })
    //        {
    //            var maximumCopyable = MaximumCopyable(buffer);
    //            return maximumCopyable;
    //        }

    //        if (!await _resultChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
    //            return 0; // No more data 

    //        if (!_resultChannel.Reader.TryRead(out var readen))
    //        {
    //            throw new EndOfStreamException($"Channel completed prematurely"); 
    //        }

    //        if (readen is null)
    //        {
    //            return 0;
    //        }

    //        _remaining = readen;
    //        var result = MaximumCopyable(buffer);

    //        return result;
    //    }

    //    private int MaximumCopyable(Memory<byte> buffer)
    //    {
    //        var maximumCopyable = Math.Min(_remaining.Memory.Length, buffer.Length);

    //        _remaining.Memory.Slice(0, maximumCopyable).CopyTo(buffer);
    //        _remaining.Memory = _remaining.Memory.Slice(maximumCopyable);

    //        if (_remaining.Memory.IsEmpty)
    //        {
    //            _remaining.Dispose();
    //            _remaining = null;
    //        }

    //        return maximumCopyable;
    //    }
        

    //    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    //    {
    //        return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    //    }

    //    [Obsolete("" + nameof(ReadAsync))]
    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        throw new InvalidOperationException($"This stream can only be read asynchronously. Consider using {nameof(ReadAsync)}");
    //    }

    //    [Obsolete("Not Supported")]
    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    [Obsolete("Not Supported")]
    //    public override void SetLength(long value)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    [Obsolete("This stream is readonly")]
    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public override bool CanRead => !_done;

    //    public override bool CanSeek => false;

    //    public override bool CanWrite => false;

    //    public override long Length => throw new NotSupportedException();

    //    public override long Position
    //    {
    //        get => _offset;
    //        [Obsolete("This stream is not seekable")]
    //        set => throw new NotSupportedException();
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        _resultChannel.Writer.TryComplete();
    //        base.Dispose(disposing);
    //    }

    //    public override ValueTask DisposeAsync()
    //    {
    //        _resultChannel.Writer.TryComplete(); 
    //        return base.DisposeAsync();
    //    }
    //}
}