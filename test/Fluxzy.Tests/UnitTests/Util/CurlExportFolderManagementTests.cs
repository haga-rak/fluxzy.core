// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Utils.Curl;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class CurlExportFolderManagementTests : ProduceDeletableItem
    {
        [Fact]
        public async Task SaveTo()
        {
            var directoryName = GetRegisteredRandomDirectory();

            var curlExportFolderManagement = new CurlExportFolderManagement(directoryName);
            var uid = new Guid("b3e3f2a0-0b1a-4e1a-9e1a-9e1a9e1a9e1a");

            File.WriteAllText(curlExportFolderManagement.GetTemporaryPathFor(uid), "yo");

            var result = await curlExportFolderManagement.SaveTo(uid, "test");
            using var stream = curlExportFolderManagement.GetTemporaryFileStream(uid);

            Assert.True(result);
            Assert.NotNull(stream);
        }
    }
}
