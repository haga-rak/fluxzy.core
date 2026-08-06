// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H2
{
    internal readonly struct WriteTask
    {
        public WriteTask(
            H2FrameType frameType,
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
            CompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public ReadOnlyMemory<byte> BufferBytes { get; }

        public void OnComplete(Exception? ex)
        {
            if (ex is OperationCanceledException cancellation) {
                if (cancellation.CancellationToken.CanBeCanceled)
                    CompletionSource.TrySetCanceled(cancellation.CancellationToken);
                else
                    CompletionSource.TrySetCanceled();

                return;
            }

            if (ex != null) {
                if (CompletionSource.TrySetException(ex)) {
                    // Fire and forget frames (RST, ping, window updates,
                    // settings ack) never await DoneTask. Read Exception so a
                    // faulted task cannot raise UnobservedTaskException.
                    _ = CompletionSource.Task.Exception;
                }

                return;
            }

            CompletionSource.TrySetResult(null);
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
