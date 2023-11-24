// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    /// <summary>
    ///    Base exception for Fluxzy connect error to a remote endpoint 
    /// </summary>
    public class FluxzyException : Exception
    {
        public FluxzyException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// The target host if any 
        /// </summary>
        public string?  TargetHost { get; set; }
    }
}
