// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public class DirectoryArchiveReaderTests : ArchiveReaderTests
    {
        public DirectoryArchiveReaderTests()
            : base(new DirectoryArchiveReader(".artefacts/tests/pink-floyd"))
        {
        }
    }
}
