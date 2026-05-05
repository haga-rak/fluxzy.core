// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public class CapturedSettingTests : IDisposable
    {
        private readonly string _baseDirectory;

        public CapturedSettingTests()
        {
            _baseDirectory = Path.Combine(Path.GetTempPath(),
                "fluxzy-capturedsetting-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Dispose()
        {
            try {
                if (Directory.Exists(_baseDirectory))
                    Directory.Delete(_baseDirectory, recursive: true);
            }
            catch {
                // best effort cleanup
            }
        }

        private static FluxzySetting BuildRichSetting()
        {
            var setting = FluxzySetting.CreateDefault();
            setting.SetConnectionPerHost(7);
            setting.SetExpectContinueTimeout(TimeSpan.FromSeconds(3));
            setting.SetSaveFilter(new HostFilter("example.com"));
            setting.AddAlterationRules(new AddRequestHeaderAction("X-Test", "value"),
                new HostFilter("example.com"));
            return setting;
        }

        [Fact]
        public void Captured_Setting_Round_Trip_Directory()
        {
            var setting = BuildRichSetting();

            var writer = new DirectoryArchiveWriter(_baseDirectory, saveFilter: null, capturedSetting: setting);
            writer.Init();
            writer.SetResolvedEndPoints(new[] { new IPEndPoint(IPAddress.Loopback, 12345) });
            writer.Dispose();

            var reader = new DirectoryArchiveReader(_baseDirectory);
            var meta = reader.ReadMetaInformation();

            Assert.NotNull(meta.CapturedSetting);
            Assert.Equal(7, meta.CapturedSetting!.ConnectionPerHost);
            Assert.Equal(TimeSpan.FromSeconds(3), meta.CapturedSetting.ExpectContinueTimeout);
            Assert.NotNull(meta.CapturedSetting.SaveFilter);
            Assert.Single(meta.CapturedSetting.AlterationRules);

            Assert.Single(meta.ResolvedEndPoints);
            Assert.Equal(12345, meta.ResolvedEndPoints[0].Port);

            Assert.Equal("0.3.0", meta.ArchiveVersion);
        }

        [Fact]
        public void Captured_Setting_Redacts_Secrets()
        {
            var setting = FluxzySetting.CreateDefault();
            setting.SetCaCertificate(Certificate.LoadFromPkcs12("/tmp/secret.p12", "super-secret-passphrase"));
            setting.SetProxyAuthentication(ProxyAuthentication.Basic("alice", "hunter2"));
            setting.SetCertificateCacheDirectory("/home/alice/.fluxzy/cache");

            var writer = new DirectoryArchiveWriter(_baseDirectory, saveFilter: null, capturedSetting: setting);
            writer.Init();
            writer.Dispose();

            var metaPath = Path.Combine(_baseDirectory, "meta.json");
            var raw = File.ReadAllText(metaPath);

            Assert.DoesNotContain("super-secret-passphrase", raw);
            Assert.DoesNotContain("hunter2", raw);
            Assert.DoesNotContain("/tmp/secret.p12", raw);
            Assert.DoesNotContain("/home/alice/.fluxzy/cache", raw);

            var reader = new DirectoryArchiveReader(_baseDirectory);
            var meta = reader.ReadMetaInformation();

            Assert.NotNull(meta.CapturedSetting);
            Assert.Null(meta.CapturedSetting!.CaCertificate.Pkcs12File);
            Assert.Null(meta.CapturedSetting.CaCertificate.Pkcs12Password);
            Assert.NotNull(meta.CapturedSetting.ProxyAuthentication);
            Assert.Equal("alice", meta.CapturedSetting.ProxyAuthentication!.Username);
            Assert.Null(meta.CapturedSetting.ProxyAuthentication.Password);
        }

        [Fact]
        public async Task Captured_Setting_Round_Trip_Fxzy_Zip()
        {
            var setting = BuildRichSetting();

            var writer = new DirectoryArchiveWriter(_baseDirectory, saveFilter: null, capturedSetting: setting);
            writer.Init();
            writer.Dispose();

            var fxzyPath = Path.Combine(_baseDirectory, "..", Guid.NewGuid().ToString("N") + ".fxzy");
            var packager = new FxzyDirectoryPackager();
            await packager.Pack(_baseDirectory, fxzyPath);

            try {
                using var zipReader = new FluxzyArchiveReader(fxzyPath);
                var meta = zipReader.ReadMetaInformation();

                Assert.NotNull(meta.CapturedSetting);
                Assert.Equal(7, meta.CapturedSetting!.ConnectionPerHost);
                Assert.Single(meta.CapturedSetting.AlterationRules);
            }
            finally {
                if (File.Exists(fxzyPath))
                    File.Delete(fxzyPath);
            }
        }

        [Fact]
        public void Captured_Setting_Null_When_Absent_Legacy_Archive()
        {
            using var reader = new FluxzyArchiveReader("_Files/Archives/with-request-payload.fxzy");
            var meta = reader.ReadMetaInformation();

            Assert.Null(meta.CapturedSetting);
        }

        [Fact]
        public void RedactSettingsInArchive_Drops_Rules_And_SaveFilter()
        {
            var setting = BuildRichSetting();

            var previous = FluxzySharedSetting.RedactSettingsInArchive;
            FluxzySharedSetting.RedactSettingsInArchive = true;

            try {
                var writer = new DirectoryArchiveWriter(_baseDirectory, saveFilter: null, capturedSetting: setting);
                writer.Init();
                writer.Dispose();

                var reader = new DirectoryArchiveReader(_baseDirectory);
                var meta = reader.ReadMetaInformation();

                Assert.NotNull(meta.CapturedSetting);
                Assert.Empty(meta.CapturedSetting!.AlterationRules);
                Assert.Null(meta.CapturedSetting.SaveFilter);
            }
            finally {
                FluxzySharedSetting.RedactSettingsInArchive = previous;
            }
        }

        [Fact]
        public void Captured_Setting_Null_When_Not_Provided()
        {
            var writer = new DirectoryArchiveWriter(_baseDirectory, saveFilter: null);
            writer.Init();
            writer.Dispose();

            var reader = new DirectoryArchiveReader(_baseDirectory);
            var meta = reader.ReadMetaInformation();

            Assert.Null(meta.CapturedSetting);
        }
    }
}
