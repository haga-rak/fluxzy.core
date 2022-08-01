using System;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public class HPackCodecException : Exception
    {
        public HPackCodecException(string message)
            : base(message)
        {

        }
        public HPackCodecException(string message, Exception exception)
            : base(message, exception)
        {

        }
    }


    public class FluxzyException : Exception
    {
        public FluxzyException(string message)
            : base(message)
        {

        }
        public FluxzyException(string message, Exception exception)
            : base(message, exception)
        {

        }
    }
}