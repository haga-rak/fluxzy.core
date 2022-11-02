// Copyright © 2021 Haga Rakotoharivelo

using System;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    public class H2Exception : Exception
    {
        public H2Exception(string message, H2ErrorCode errorCode, Exception? innerException = null) :
            base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Error code according to https://httpwg.org/specs/rfc7540.html#ErrorCodes
        /// 
        /// </summary>
        public H2ErrorCode ErrorCode { get; }
    }

    public class ExchangeException : Exception
    {
        public ExchangeException(string message, Exception innerException = null) :
            base(message, innerException)
        {

        }
    }
}