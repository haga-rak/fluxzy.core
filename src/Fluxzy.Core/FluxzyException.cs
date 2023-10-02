// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public class FluxzyException : Exception
    {
        public FluxzyException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public string?  TargetHost { get; set; }
    }
}
