// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public class FluxzyArchiveReaderTests : ArchiveReaderTests
    {
        public FluxzyArchiveReaderTests()
            : base(new FluxzyArchiveReader("_Files/Archives/with-request-payload.fxzy"))
        {
        }
    }
}
