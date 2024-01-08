using System;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Packagers
{
    public class DefaultExport : ProduceDeletableItem
    {
        [Fact]
        [Obsolete]
        public void Export_Saz()
        {
            var directory = ".artefacts/tests/pink-floyd";
            var fileName = GetRegisteredRandomFile();

            Packager.ExportAsSaz(directory, fileName);
        }

        [Fact]
        public void Export_Har()
        {
            var directory = ".artefacts/tests/pink-floyd";
            var fileName = GetRegisteredRandomFile();

            Packager.ExportAsHttpArchive(directory, fileName);
        }
    }
}
