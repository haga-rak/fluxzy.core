// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Writers
{
    public class PcapOnlyArchiveWriter : EventOnlyArchiveWriter
    {
        private readonly Func<int, string> _pcapFileFunc;

        public PcapOnlyArchiveWriter(Func<int, string> pcapFileFunc)
        {
            _pcapFileFunc = pcapFileFunc;
        }

        public override string GetDumpfilePath(int connectionId)
        {
            return _pcapFileFunc(connectionId);
        }
    }
}
