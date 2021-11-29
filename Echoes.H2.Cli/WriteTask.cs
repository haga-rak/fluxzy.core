// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class WriteTask
    {
        public WriteTask(ReadOnlyMemory<byte> bufferBytes, 
            int streamIdentifier, 
            int priority, int streamDependency)
        {
            BufferBytes = bufferBytes;
            StreamIdentifier = streamIdentifier;
            Priority = priority;
            StreamDependency = streamDependency;
        }

        private readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>(); 

        public ReadOnlyMemory<byte>  BufferBytes { get; set; }
        
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

        public int StreamIdentifier { get; set; }

        public int Priority { get; set; } = 0;

        public int StreamDependency { get; set; }
    }
}