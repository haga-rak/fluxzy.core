using System.Threading;

namespace Echoes.H2.Tests.Utils
{
    public static class PortProvider
    {
        private static int _portCounter = 14621; 

        public static int Next()
        {
            return Interlocked.Increment(ref _portCounter); 
        }
    }
}