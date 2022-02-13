using System;

namespace Echoes
{
    public class EchoesException : Exception
    {
        public EchoesException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
