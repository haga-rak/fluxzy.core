// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public class ConnectionCloseException : FluxzyException
    {
        public ConnectionCloseException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
