// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;

namespace Echoes.Helpers
{
    internal static class TaskCompletionExtensions
    {
        public static bool TryEnd<T>(this TaskCompletionSource<T> taskCompletionSource, Exception ex = null)
        {
            if (taskCompletionSource.Task.IsCompleted)
                return false;

            
            if (ex == null)
            {
                taskCompletionSource.SetCanceled();
                return true; 
            }

            taskCompletionSource.SetException(ex);

            return true; 
        }
    }
}