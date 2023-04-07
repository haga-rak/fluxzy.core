// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class SazArchive
    {
        [Fact]
        public void TestValidArchiveFile()
        {
            var outputDirectory = "archive-saz-test"; 
            var inputFile = "archive-saz-test.saz";

            File.WriteAllBytes(inputFile, StorageContext.testarchive);

            try {
                var sazArchiveReader = new SazArchiveReader();

                Assert.True(sazArchiveReader.IsSazArchive(inputFile));
            }
            finally {

                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, true);

                if (File.Exists(inputFile))
                    File.Delete(inputFile);
            }
        }


        [Fact]
        public void TestMinimalArchive()
        {

            var outputDirectory = "minimal-archive-saz-test";
            var inputFile = "minimal-archive-saz-test.saz";

            File.WriteAllBytes(inputFile, StorageContext.minimal);

            try
            {
                var sazArchiveReader = new SazArchiveReader();

                sazArchiveReader.WriteToDirectory(inputFile, outputDirectory);
            }
            finally
            {

                //if (Directory.Exists(outputDirectory))
                //    Directory.Delete(outputDirectory, true);

                //if (File.Exists(inputFile))
                //    File.Delete(inputFile);
            }
        }
    }
}
