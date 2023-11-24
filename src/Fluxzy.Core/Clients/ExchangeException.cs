// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients
{
    /// <summary>
    /// 
    /// </summary>
    public class ExchangeException : Exception
    {
        public ExchangeException(string message, Exception? innerException = null)
            :
            base(message, innerException)
        {
        }
    }
}
