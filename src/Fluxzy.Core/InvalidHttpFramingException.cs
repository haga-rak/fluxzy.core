// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    /// <summary>
    ///     Thrown when an HTTP/1.1 message declares ambiguous or invalid body framing
    ///     (conflicting or duplicate Content-Length values, or a non 1*DIGIT value).
    ///     The connection is dropped to prevent request/response smuggling
    ///     (RFC 7230 §3.3.3).
    /// </summary>
    public class InvalidHttpFramingException : FluxzyException
    {
        public InvalidHttpFramingException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
