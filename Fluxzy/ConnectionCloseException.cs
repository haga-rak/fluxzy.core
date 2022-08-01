using System;

namespace Fluxzy
{
    public class ConnectionCloseException : FluxzyException
    {
        public ConnectionCloseException(
            string message, Exception innerException = null) 
            : base(message, innerException)
        {

        }
    }
}
