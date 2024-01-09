using System.Net;
using System.Text.Json;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Settings
{
    public class FluxzySettingTests
    {
        /// <summary>
        /// Ensure serialization runs OK
        /// </summary>
        [Fact]
        public void Read_Write()
        {
            var settings = FluxzySetting.CreateDefault();

            settings.AddBoundAddress(IPAddress.IPv6Any, 652);

            var jsonString = JsonSerializer.Serialize(settings, GlobalArchiveOption.ConfigSerializerOptions);

            var newSettings = JsonSerializer.Deserialize<FluxzySetting>(jsonString,
                GlobalArchiveOption.ConfigSerializerOptions);

            Assert.NotNull(newSettings!);
            Assert.Equal(settings.ArchivingPolicy, newSettings.ArchivingPolicy);
            Assert.Equal(settings.BoundPointsDescription, newSettings.BoundPointsDescription);
            Assert.Equal(settings.BoundPoints, newSettings.BoundPoints);
            Assert.Equal(settings.ByPassHostFlat, newSettings.ByPassHostFlat);
            Assert.Equal(settings.CaptureInterfaceName, newSettings.CaptureInterfaceName);
            Assert.Equal(settings.CaptureRawPacket, newSettings.CaptureRawPacket);
            Assert.Equal(settings.AutoInstallCertificate, newSettings.AutoInstallCertificate);
            Assert.Equal(settings.UseBouncyCastle, newSettings.UseBouncyCastle);
            Assert.Equal(settings.ServerProtocols, newSettings.ServerProtocols);
            Assert.Equal(settings.CaCertificate, newSettings.CaCertificate);
        }
    }
}
