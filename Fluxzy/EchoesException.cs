using System;

namespace Fluxzy
{
    public class EchoesException : Exception
    {
        public EchoesException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class ConnectionCloseException : EchoesException
    {
        public ConnectionCloseException(
            string message, Exception innerException = null) 
            : base(message, innerException)
        {

        }
    }
}
