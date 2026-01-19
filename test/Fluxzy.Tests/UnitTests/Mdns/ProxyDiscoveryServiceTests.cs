// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Mdns
{
    public class ProxyDiscoveryServiceTests
    {
        private static MdnsAnnouncerOptions CreateTestOptions() => new()
        {
            ServiceName = "TestProxy",
            ProxyPort = 9852,
            HostIpAddress = "127.0.0.1",
            HostName = "TestHost",
            OsName = "TestOS",
            FluxzyVersion = "1.0.0",
            FluxzyStartupSetting = "Test settings",
            CertEndpoint = "/ca",
            InitialAnnouncementCount = 1,
            InitialAnnouncementDelayMs = 10
        };

        [Fact]
        public async Task Should_Create_Service_With_Valid_Options()
        {
            // Arrange & Act
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Assert
            Assert.NotNull(service);
            Assert.False(service.IsRunning);
        }

        [Fact]
        public void Should_Throw_For_Invalid_Ip_Address()
        {
            // Arrange
            var options = CreateTestOptions() with { HostIpAddress = "invalid" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ProxyDiscoveryService(options));
        }

        [Fact]
        public void Should_Throw_For_IPv6_Address()
        {
            // Arrange
            var options = CreateTestOptions() with { HostIpAddress = "::1" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ProxyDiscoveryService(options));
        }

        [Fact]
        public void Should_Throw_For_Null_Options()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProxyDiscoveryService(null!));
        }

        [Fact]
        public async Task Should_Report_IsRunning_Correctly_Before_Start()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Assert
            Assert.False(service.IsRunning);
        }

        [Fact]
        public async Task Should_Not_Throw_When_Stop_Called_Without_Start()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act & Assert - Should not throw
            await service.StopAsync();
            Assert.False(service.IsRunning);
        }

        [Fact]
        public async Task Should_Not_Throw_When_Disposed_Twice()
        {
            // Arrange
            var options = CreateTestOptions();
            var service = new ProxyDiscoveryService(options);

            // Act & Assert - Should not throw
            await service.DisposeAsync();
            await service.DisposeAsync();
        }

        [Fact]
        public async Task Should_Build_Correct_Announcement_Packet()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act
            var packet = service.BuildAnnouncementPacket();

            // Assert
            Assert.NotEmpty(packet);

            // Verify header
            var flags = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2, 2));
            Assert.Equal(MdnsConstants.ResponseFlags, flags);

            var answerCount = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(6, 2));
            Assert.Equal(4, answerCount);

            // Find first record's TTL (after header + name + type + class)
            var ttl = FindFirstRecordTtl(packet);
            Assert.True(ttl > 0, "Announcement packet should have TTL > 0");
        }

        [Fact]
        public async Task Should_Build_Correct_Goodbye_Packet()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act
            var packet = service.BuildGoodbyePacket();

            // Assert
            Assert.NotEmpty(packet);

            // Verify header
            var flags = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2, 2));
            Assert.Equal(MdnsConstants.ResponseFlags, flags);

            // Find first record's TTL - should be 0 for goodbye
            var ttl = FindFirstRecordTtl(packet);
            Assert.Equal(0u, ttl);
        }

        [Fact]
        public async Task Should_Include_Correct_Metadata_In_Packet()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act
            var packet = service.BuildAnnouncementPacket();

            // Assert - The packet should contain the service name
            var packetString = System.Text.Encoding.ASCII.GetString(packet);
            Assert.Contains("TestProxy", packetString);
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task Should_Start_And_Stop_Without_Error()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act & Assert - This may throw on systems without network
            try
            {
                await service.StartAsync();
                Assert.True(service.IsRunning);

                await service.StopAsync();
                Assert.False(service.IsRunning);
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Expected on systems without proper network configuration
                // or when running in CI without network access
            }
        }

        [Trait("Category", "Integration")]
        [Fact]
        public async Task Should_Not_Throw_When_Start_Called_Twice()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            try
            {
                // Act
                await service.StartAsync();
                await service.StartAsync(); // Second call should be idempotent

                // Assert
                Assert.True(service.IsRunning);
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Expected on systems without proper network configuration
            }
        }

        private static uint FindFirstRecordTtl(byte[] packet)
        {
            // Skip header (12 bytes)
            var offset = 12;

            // Skip the name (find null terminator)
            while (offset < packet.Length && packet[offset] != 0)
            {
                offset += packet[offset] + 1;
            }
            offset++; // Skip null terminator

            // Skip type (2 bytes) and class (2 bytes)
            offset += 4;

            // Read TTL (4 bytes, big-endian)
            return BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(offset, 4));
        }

        #region Query Response Tests

        [Fact]
        public void Should_Detect_Ptr_Query_For_Our_Service_Type()
        {
            // Arrange
            var ptrQuery = BuildPtrQueryPacket(MdnsConstants.ServiceType);
            DnsPacketParser.TryParse(ptrQuery, out var packetInfo);

            // Act
            var isPtrQuery = DnsPacketParser.IsPtrQueryForService(
                packetInfo,
                MdnsConstants.ServiceType);

            // Assert
            Assert.True(isPtrQuery);
        }

        [Fact]
        public void Should_Detect_Query_For_Our_Instance()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            var srvQuery = BuildQueryPacket(instanceName, MdnsConstants.TypeSRV);
            DnsPacketParser.TryParse(srvQuery, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Detect_Query_For_Our_Host()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            var aQuery = BuildQueryPacket(hostFqdn, MdnsConstants.TypeA);
            DnsPacketParser.TryParse(aQuery, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Not_Respond_To_Response_Packets()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            var responsePacket = BuildResponsePacket();
            DnsPacketParser.TryParse(responsePacket, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.False(isQueryForService);
        }

        [Fact]
        public void Should_Not_Respond_To_Queries_For_Other_Services()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            var otherServiceQuery = BuildPtrQueryPacket("_http._tcp.local");
            DnsPacketParser.TryParse(otherServiceQuery, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.False(isQueryForService);
        }

        [Fact]
        public async Task Should_Parse_Own_Announcement_Packet()
        {
            // Arrange
            var options = CreateTestOptions();
            await using var service = new ProxyDiscoveryService(options);

            // Act
            var packet = service.BuildAnnouncementPacket();
            var success = DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert
            Assert.True(success);
            Assert.True(packetInfo.IsResponse);
            Assert.False(packetInfo.IsQuery);
        }

        [Fact]
        public void Should_Handle_Case_Insensitive_Service_Matching()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            // Query with uppercase
            var query = BuildPtrQueryPacket("_FLUXZYPROXY._TCP.LOCAL");
            DnsPacketParser.TryParse(query, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Handle_Txt_Query_For_Instance()
        {
            // Arrange
            var options = CreateTestOptions();
            var instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            var hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";

            var txtQuery = BuildQueryPacket(instanceName, MdnsConstants.TypeTXT);
            DnsPacketParser.TryParse(txtQuery, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                MdnsConstants.ServiceType,
                instanceName,
                hostFqdn);

            // Assert
            Assert.True(isQueryForService);
        }

        #endregion

        #region Helper Methods

        private static byte[] BuildPtrQueryPacket(string name)
        {
            return BuildQueryPacket(name, MdnsConstants.TypePTR);
        }

        private static byte[] BuildQueryPacket(string name, ushort type)
        {
            var data = new List<byte>();

            // Header
            data.AddRange(new byte[] { 0, 0 });   // Transaction ID
            data.AddRange(new byte[] { 0, 0 });   // Flags (query)
            data.AddRange(new byte[] { 0, 1 });   // Question count = 1
            data.AddRange(new byte[] { 0, 0 });   // Answer count
            data.AddRange(new byte[] { 0, 0 });   // Authority count
            data.AddRange(new byte[] { 0, 0 });   // Additional count

            // Question
            data.AddRange(DnsPacketBuilder.EncodeDnsName(name));

            // Type
            var typeBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(typeBytes, type);
            data.AddRange(typeBytes);

            // Class = IN
            var classBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(classBytes, MdnsConstants.ClassIN);
            data.AddRange(classBytes);

            return data.ToArray();
        }

        private static byte[] BuildResponsePacket()
        {
            var data = new List<byte>();

            // Header with QR=1 (response)
            data.AddRange(new byte[] { 0, 0 });    // Transaction ID
            data.AddRange(new byte[] { 0x84, 0 }); // Flags: QR=1, AA=1
            data.AddRange(new byte[] { 0, 0 });    // Question count
            data.AddRange(new byte[] { 0, 0 });    // Answer count
            data.AddRange(new byte[] { 0, 0 });    // Authority count
            data.AddRange(new byte[] { 0, 0 });    // Additional count

            return data.ToArray();
        }

        #endregion
    }
}
