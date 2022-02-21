using System;
using System.Threading;

namespace Echoes.H2.Tests.Utils
{
    public static class PortProvider
    {
        private static int _portCounter = Random.Shared.Next(16000, 40000); 

        public static int Next()
        {
            return Interlocked.Increment(ref _portCounter); 
        }
    }
}