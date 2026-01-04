// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Net;
using System.Text;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Mdns
{
    public class DnsPacketBuilderTests
    {
        [Fact]
        public void Should_Encode_DnsName_Correctly()
        {
            // Arrange & Act
            var encoded = DnsPacketBuilder.EncodeDnsName("test.local");

            // Assert
            // Expected: [4]test[5]local[0]
            Assert.Equal(12, encoded.Length);
            Assert.Equal(4, encoded[0]);  // Length of "test"
            Assert.Equal((byte)'t', encoded[1]);
            Assert.Equal((byte)'e', encoded[2]);
            Assert.Equal((byte)'s', encoded[3]);
            Assert.Equal((byte)'t', encoded[4]);
            Assert.Equal(5, encoded[5]);  // Length of "local"
            Assert.Equal((byte)'l', encoded[6]);
            Assert.Equal((byte)'o', encoded[7]);
            Assert.Equal((byte)'c', encoded[8]);
            Assert.Equal((byte)'a', encoded[9]);
            Assert.Equal((byte)'l', encoded[10]);
            Assert.Equal(0, encoded[11]); // Null terminator
        }

        [Fact]
        public void Should_Encode_DnsName_With_Multiple_Labels()
        {
            // Arrange & Act
            var encoded = DnsPacketBuilder.EncodeDnsName("_fluxzyproxy._tcp.local");

            // Assert
            // Expected: [12]_fluxzyproxy[4]_tcp[5]local[0]
            Assert.Equal(25, encoded.Length);
            Assert.Equal(12, encoded[0]);  // Length of "_fluxzyproxy"
            Assert.Equal(4, encoded[13]);  // Length of "_tcp"
            Assert.Equal(5, encoded[18]);  // Length of "local"
            Assert.Equal(0, encoded[24]);  // Null terminator
        }

        [Fact]
        public void Should_Encode_Empty_DnsName()
        {
            // Arrange & Act
            var encoded = DnsPacketBuilder.EncodeDnsName("");

            // Assert
            Assert.Single(encoded);
            Assert.Equal(0, encoded[0]);
        }

        [Fact]
        public void Should_Encode_Empty_TxtRecord()
        {
            // Arrange & Act
            var encoded = DnsPacketBuilder.EncodeTxtRecord("");

            // Assert
            Assert.Single(encoded);
            Assert.Equal(0, encoded[0]);
        }

        [Fact]
        public void Should_Encode_TxtRecord_With_KeyValue()
        {
            // Arrange
            var text = "key=value";

            // Act
            var encoded = DnsPacketBuilder.EncodeTxtRecord(text);

            // Assert
            Assert.Equal(10, encoded.Length); // 1 byte length + 9 bytes text
            Assert.Equal(9, encoded[0]);      // Length prefix
            Assert.Equal("key=value", Encoding.UTF8.GetString(encoded, 1, 9));
        }

        [Fact]
        public void Should_Encode_TxtRecord_With_Json()
        {
            // Arrange
            var json = "{\"host\":\"192.168.1.100\",\"port\":9852}";

            // Act
            var encoded = DnsPacketBuilder.EncodeTxtRecord(json);

            // Assert
            Assert.Equal(json.Length + 1, encoded.Length); // 1 byte length + json bytes
            Assert.Equal(json.Length, encoded[0]);
            Assert.Equal(json, Encoding.UTF8.GetString(encoded, 1, json.Length));
        }

        [Fact]
        public void Should_Split_Long_TxtRecord_Into_Chunks()
        {
            // Arrange - Create a string longer than 255 bytes
            var longText = new string('x', 300);

            // Act
            var encoded = DnsPacketBuilder.EncodeTxtRecord(longText);

            // Assert
            // Should be: [255][first 255 chars][45][remaining 45 chars]
            Assert.Equal(302, encoded.Length); // 1 + 255 + 1 + 45
            Assert.Equal(255, encoded[0]);     // First chunk length
            Assert.Equal(45, encoded[256]);    // Second chunk length
        }

        [Fact]
        public void Should_Limit_Chunk_Size_To_255_Bytes()
        {
            // Arrange - Create a string that requires multiple chunks
            var longText = new string('a', 600);

            // Act
            var encoded = DnsPacketBuilder.EncodeTxtRecord(longText);

            // Assert - Verify no chunk exceeds 255 bytes
            var offset = 0;
            while (offset < encoded.Length)
            {
                var chunkLength = encoded[offset];
                Assert.True(chunkLength <= 255, $"Chunk length {chunkLength} exceeds 255");
                offset += 1 + chunkLength;
            }
        }

        [Fact]
        public void Should_Build_Valid_Ptr_Record()
        {
            // Arrange
            var builder = new DnsPacketBuilder();
            var serviceType = "_fluxzyproxy._tcp.local";
            var instanceName = "TestProxy._fluxzyproxy._tcp.local";

            // Act
            builder.WritePtrRecord(serviceType, instanceName, 4500);
            var packet = builder.GetPacket();

            // Assert
            Assert.NotEmpty(packet);

            // Verify it starts with the service type name
            Assert.Equal(12, packet[0]); // Length of "_fluxzyproxy"

            // Find the type field (after the name)
            var nameEnd = FindNameEnd(packet, 0);
            var typeOffset = nameEnd;
            var recordType = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(typeOffset, 2));
            Assert.Equal(MdnsConstants.TypePTR, recordType);
        }

        [Fact]
        public void Should_Build_Valid_Srv_Record()
        {
            // Arrange
            var builder = new DnsPacketBuilder();
            var instanceName = "TestProxy._fluxzyproxy._tcp.local";
            var hostname = "desktop.local";

            // Act
            builder.WriteSrvRecord(instanceName, hostname, 9852, 4500);
            var packet = builder.GetPacket();

            // Assert
            Assert.NotEmpty(packet);

            // Find the type field
            var nameEnd = FindNameEnd(packet, 0);
            var typeOffset = nameEnd;
            var recordType = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(typeOffset, 2));
            Assert.Equal(MdnsConstants.TypeSRV, recordType);

            // Verify port is in the RDATA (after name + type + class + ttl + rdlength)
            var rdataOffset = nameEnd + 2 + 2 + 4 + 2; // type + class + ttl + rdlength
            var port = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(rdataOffset + 4, 2)); // priority + weight + port
            Assert.Equal(9852, port);
        }

        [Fact]
        public void Should_Build_Valid_A_Record()
        {
            // Arrange
            var builder = new DnsPacketBuilder();
            var hostname = "desktop.local";
            var ipAddress = IPAddress.Parse("192.168.1.100");

            // Act
            builder.WriteARecord(hostname, ipAddress, 4500);
            var packet = builder.GetPacket();

            // Assert
            Assert.NotEmpty(packet);

            // Find the type field
            var nameEnd = FindNameEnd(packet, 0);
            var typeOffset = nameEnd;
            var recordType = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(typeOffset, 2));
            Assert.Equal(MdnsConstants.TypeA, recordType);

            // Verify IP address in RDATA
            var rdataOffset = nameEnd + 2 + 2 + 4 + 2; // type + class + ttl + rdlength
            Assert.Equal(192, packet[rdataOffset]);
            Assert.Equal(168, packet[rdataOffset + 1]);
            Assert.Equal(1, packet[rdataOffset + 2]);
            Assert.Equal(100, packet[rdataOffset + 3]);
        }

        [Fact]
        public void Should_Use_Network_Byte_Order()
        {
            // Arrange
            var builder = new DnsPacketBuilder();

            // Act
            builder.WriteUInt16(0x1234);
            builder.WriteUInt32(0x12345678);
            var packet = builder.GetPacket();

            // Assert - Verify big-endian encoding
            Assert.Equal(0x12, packet[0]); // High byte first for UInt16
            Assert.Equal(0x34, packet[1]);
            Assert.Equal(0x12, packet[2]); // High byte first for UInt32
            Assert.Equal(0x34, packet[3]);
            Assert.Equal(0x56, packet[4]);
            Assert.Equal(0x78, packet[5]);
        }

        [Fact]
        public void Should_Build_Complete_Announcement_Packet()
        {
            // Arrange
            var serviceName = "TestProxy";
            var hostname = "desktop";
            var ipAddress = IPAddress.Parse("192.168.1.100");
            ushort port = 9852;
            var txtData = "{\"test\":\"value\"}";

            // Act
            var packet = DnsPacketBuilder.BuildAnnouncementPacket(
                serviceName, hostname, ipAddress, port, txtData);

            // Assert
            Assert.NotEmpty(packet);

            // Verify header
            var flags = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2, 2));
            Assert.Equal(MdnsConstants.ResponseFlags, flags);

            var answerCount = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(6, 2));
            Assert.Equal(4, answerCount);
        }

        [Fact]
        public void Should_Build_Goodbye_Packet_With_Zero_Ttl()
        {
            // Arrange
            var serviceName = "TestProxy";
            var hostname = "desktop";
            var ipAddress = IPAddress.Parse("192.168.1.100");
            ushort port = 9852;
            var txtData = "{\"test\":\"value\"}";

            // Act
            var packet = DnsPacketBuilder.BuildGoodbyePacket(
                serviceName, hostname, ipAddress, port, txtData);

            // Assert
            Assert.NotEmpty(packet);

            // Find TTL in first record (after header + name + type + class)
            var offset = 12; // Skip header
            var nameEnd = FindNameEnd(packet, offset);
            var ttlOffset = nameEnd + 2 + 2; // type + class
            var ttl = BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(ttlOffset, 4));
            Assert.Equal(0u, ttl);
        }

        [Fact]
        public void Should_Throw_For_IPv6_Address()
        {
            // Arrange
            var builder = new DnsPacketBuilder();
            var ipv6Address = IPAddress.Parse("::1");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.WriteARecord("test.local", ipv6Address, 4500));
        }

        [Fact]
        public void Should_Throw_For_Label_Exceeding_63_Characters()
        {
            // Arrange
            var longLabel = new string('x', 64);
            var name = $"{longLabel}.local";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DnsPacketBuilder.EncodeDnsName(name));
        }

        private static int FindNameEnd(byte[] packet, int start)
        {
            var offset = start;
            while (offset < packet.Length && packet[offset] != 0)
            {
                offset += packet[offset] + 1;
            }
            return offset + 1; // Include the null terminator
        }
    }
}
