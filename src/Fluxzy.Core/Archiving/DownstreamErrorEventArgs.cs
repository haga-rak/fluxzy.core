// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public class DownstreamErrorEventArgs : EventArgs
    {
        public DownstreamErrorEventArgs(int count)
        {
            Count = count;
        }
        public int Count { get; }
    }
}
