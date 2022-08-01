// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy;

public class FluxzyException : Exception
{
    public FluxzyException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}