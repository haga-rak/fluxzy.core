using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using Fluxzy.Certificates;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
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

            // TODO make, distinct tests for each method
            
            settings.AddBoundAddress(IPAddress.IPv6Any, 652);
            settings.AddBoundAddress("127.0.0.1", 54652);
            settings.ClearSaveFilter();
            settings.SetSaveFilter(new HostFilter("google.com"));
            settings.SetByPassedHosts("google.com", "facebook.com");
            settings.AddTunneledHosts("microsoft.com", "apple.com");
            settings.SetConnectionPerHost(9);
            settings.SetServerProtocols(SslProtocols.Tls12);
            settings.SetCheckCertificateRevocation(false);
            settings.SetCaCertificate(Certificate.UseDefault());
            settings.AddAlterationRules(new NoOpAction(), AnyFilter.Default);
            settings.SetVerbose(true);
            settings.UseBouncyCastleSslEngine();
            settings.SetCertificateCacheDirectory("/temp"); 
            settings.SetProxyAuthentication(ProxyAuthentication.Basic("userName", "Password")); 

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
