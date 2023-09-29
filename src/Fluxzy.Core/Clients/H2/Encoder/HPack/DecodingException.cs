// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public class HPackCodecException : Exception
    {
        public HPackCodecException(string message)
            : base(message)
        {
        }
    }
}
