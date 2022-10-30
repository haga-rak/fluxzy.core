// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H2
{
    internal readonly struct WriteTask
    {
        public WriteTask(H2FrameType frameType,
            int streamIdentifier,
            int priority,
            int streamDependency,
            ReadOnlyMemory<byte> bufferBytes,
            int value = 0)
        {
            BufferBytes = bufferBytes;
            StreamIdentifier = streamIdentifier;
            Priority = priority;
            StreamDependency = streamDependency;
            FrameType = frameType;
            WindowUpdateSize = value;
            CompletionSource = new TaskCompletionSource<object?>();
        }

        public ReadOnlyMemory<byte> BufferBytes { get; }

        public void OnComplete(Exception? ex)
        {
            if (ex != null)
            {
                CompletionSource.SetException(ex);
                return;
            }

            CompletionSource.SetResult(null);
        }

        public Task DoneTask => CompletionSource.Task;

        public int StreamIdentifier { get; }

        public int Priority { get; }

        public int StreamDependency { get; }

        public H2FrameType FrameType { get; }

        public int WindowUpdateSize { get; }

        public TaskCompletionSource<object?> CompletionSource { get; }
    }
}