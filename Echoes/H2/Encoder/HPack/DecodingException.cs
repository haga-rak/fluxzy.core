using System;

namespace Echoes.H2.Encoder.HPack
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


    public class EchoesException : Exception
    {
        public EchoesException(string message)
            : base(message)
        {

        }
        public EchoesException(string message, Exception exception)
            : base(message, exception)
        {

        }
    }
}