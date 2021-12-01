// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;

namespace Echoes.H2
{
    internal readonly struct WriteTask
    {
        private readonly TaskCompletionSource<object> _taskCompletionSource;

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
            _taskCompletionSource = new TaskCompletionSource<object>();
        }

        public ReadOnlyMemory<byte>  BufferBytes { get;  }
        
        public void OnComplete(Exception ex)
        {
            if (ex != null)
            {
                _taskCompletionSource.SetException(ex);
                return; 
            }

            _taskCompletionSource.SetResult(null);
        }

        public Task DoneTask => _taskCompletionSource.Task;

        public int StreamIdentifier { get;  }

        public int Priority { get;  }

        public int StreamDependency { get;  }

        public H2FrameType FrameType { get; }

        public int WindowUpdateSize { get; }
    }
}