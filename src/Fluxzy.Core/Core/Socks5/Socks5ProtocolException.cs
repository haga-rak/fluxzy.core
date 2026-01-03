// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core.Socks5
{
    internal class Socks5ProtocolException : Exception
    {
        public Socks5ProtocolException(string message)
            : base(message)
        {
        }

        public Socks5ProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
