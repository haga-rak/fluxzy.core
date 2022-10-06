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
}