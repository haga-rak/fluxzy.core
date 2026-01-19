// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Mdns
{
    public class DnsPacketParserTests
    {
        [Fact]
        public void Should_Parse_Valid_Query_Packet()
        {
            // Arrange - Build a simple PTR query for _fluxzyproxy._tcp.local
            var packet = BuildPtrQueryPacket("_fluxzyproxy._tcp.local");

            // Act
            var success = DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert
            Assert.True(success);
            Assert.True(packetInfo.IsQuery);
            Assert.False(packetInfo.IsResponse);
            Assert.Single(packetInfo.Questions);
            Assert.Equal("_fluxzyproxy._tcp.local", packetInfo.Questions[0].Name);
            Assert.Equal(MdnsConstants.TypePTR, packetInfo.Questions[0].Type);
            Assert.Equal(MdnsConstants.ClassIN, packetInfo.Questions[0].Class);
        }

        [Fact]
        public void Should_Parse_Response_Packet()
        {
            // Arrange - Build a simple response packet
            var packet = BuildResponsePacket();

            // Act
            var success = DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert
            Assert.True(success);
            Assert.False(packetInfo.IsQuery);
            Assert.True(packetInfo.IsResponse);
        }

        [Fact]
        public void Should_Fail_For_Too_Short_Packet()
        {
            // Arrange - Packet shorter than minimum header size
            var packet = new byte[10];

            // Act
            var success = DnsPacketParser.TryParse(packet, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void Should_Fail_For_Empty_Packet()
        {
            // Arrange
            var packet = Array.Empty<byte>();

            // Act
            var success = DnsPacketParser.TryParse(packet, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void Should_Parse_Multiple_Questions()
        {
            // Arrange - Build a packet with multiple questions
            var packet = BuildMultiQuestionPacket(new[]
            {
                ("_fluxzyproxy._tcp.local", MdnsConstants.TypePTR),
                ("test.local", MdnsConstants.TypeA)
            });

            // Act
            var success = DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert
            Assert.True(success);
            Assert.Equal(2, packetInfo.Questions.Count);
            Assert.Equal("_fluxzyproxy._tcp.local", packetInfo.Questions[0].Name);
            Assert.Equal(MdnsConstants.TypePTR, packetInfo.Questions[0].Type);
            Assert.Equal("test.local", packetInfo.Questions[1].Name);
            Assert.Equal(MdnsConstants.TypeA, packetInfo.Questions[1].Type);
        }

        [Fact]
        public void Should_Detect_Ptr_Query_For_Service()
        {
            // Arrange
            var packet = BuildPtrQueryPacket("_fluxzyproxy._tcp.local");
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isPtrQuery = DnsPacketParser.IsPtrQueryForService(packetInfo, "_fluxzyproxy._tcp.local");

            // Assert
            Assert.True(isPtrQuery);
        }

        [Fact]
        public void Should_Not_Detect_Ptr_Query_For_Different_Service()
        {
            // Arrange
            var packet = BuildPtrQueryPacket("_http._tcp.local");
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isPtrQuery = DnsPacketParser.IsPtrQueryForService(packetInfo, "_fluxzyproxy._tcp.local");

            // Assert
            Assert.False(isPtrQuery);
        }

        [Fact]
        public void Should_Not_Detect_Ptr_Query_For_Response_Packet()
        {
            // Arrange
            var packet = BuildResponsePacket();
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isPtrQuery = DnsPacketParser.IsPtrQueryForService(packetInfo, "_fluxzyproxy._tcp.local");

            // Assert
            Assert.False(isPtrQuery);
        }

        [Fact]
        public void Should_Detect_Query_For_Service_Type()
        {
            // Arrange
            var packet = BuildPtrQueryPacket("_fluxzyproxy._tcp.local");
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                "_fluxzyproxy._tcp.local",
                "TestProxy._fluxzyproxy._tcp.local",
                "testhost.local");

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Detect_Query_For_Instance_Srv()
        {
            // Arrange
            var packet = BuildQueryPacket("TestProxy._fluxzyproxy._tcp.local", MdnsConstants.TypeSRV);
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                "_fluxzyproxy._tcp.local",
                "TestProxy._fluxzyproxy._tcp.local",
                "testhost.local");

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Detect_Query_For_Instance_Txt()
        {
            // Arrange
            var packet = BuildQueryPacket("TestProxy._fluxzyproxy._tcp.local", MdnsConstants.TypeTXT);
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                "_fluxzyproxy._tcp.local",
                "TestProxy._fluxzyproxy._tcp.local",
                "testhost.local");

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Detect_Query_For_Host_A_Record()
        {
            // Arrange
            var packet = BuildQueryPacket("testhost.local", MdnsConstants.TypeA);
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                "_fluxzyproxy._tcp.local",
                "TestProxy._fluxzyproxy._tcp.local",
                "testhost.local");

            // Assert
            Assert.True(isQueryForService);
        }

        [Fact]
        public void Should_Not_Detect_Query_For_Unrelated_Service()
        {
            // Arrange
            var packet = BuildPtrQueryPacket("_http._tcp.local");
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isQueryForService = DnsPacketParser.IsQueryForService(
                packetInfo,
                "_fluxzyproxy._tcp.local",
                "TestProxy._fluxzyproxy._tcp.local",
                "testhost.local");

            // Assert
            Assert.False(isQueryForService);
        }

        [Fact]
        public void Should_Handle_Case_Insensitive_Name_Matching()
        {
            // Arrange
            var packet = BuildPtrQueryPacket("_FLUXZYPROXY._TCP.LOCAL");
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Act
            var isPtrQuery = DnsPacketParser.IsPtrQueryForService(packetInfo, "_fluxzyproxy._tcp.local");

            // Assert
            Assert.True(isPtrQuery);
        }

        [Fact]
        public void Should_Decode_Dns_Name_Correctly()
        {
            // Arrange
            var encodedName = DnsPacketBuilder.EncodeDnsName("test.local");

            // Act
            var decodedName = DnsPacketParser.DecodeDnsName(encodedName);

            // Assert
            Assert.Equal("test.local", decodedName);
        }

        [Fact]
        public void Should_Decode_Complex_Dns_Name()
        {
            // Arrange
            var encodedName = DnsPacketBuilder.EncodeDnsName("TestProxy._fluxzyproxy._tcp.local");

            // Act
            var decodedName = DnsPacketParser.DecodeDnsName(encodedName);

            // Assert
            Assert.Equal("TestProxy._fluxzyproxy._tcp.local", decodedName);
        }

        [Fact]
        public void Should_Handle_Truncated_Question_Section()
        {
            // Arrange - Build a packet with question count > 0 but no actual questions
            var packet = new byte[12];
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0), 0);     // Transaction ID
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), 0);     // Flags (query)
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4), 1);     // Question count = 1
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6), 0);     // Answer count
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(8), 0);     // Authority count
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(10), 0);    // Additional count

            // Act
            var success = DnsPacketParser.TryParse(packet, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void Should_Handle_Malformed_Name_In_Question()
        {
            // Arrange - Build a packet with an invalid name (label length > 63)
            var data = new List<byte>();

            // Header
            data.AddRange(new byte[12]);

            // Malformed name with label length 64 (> 63)
            data.Add(64);
            for (var i = 0; i < 64; i++)
                data.Add((byte)'x');
            data.Add(0);

            // Type and class
            data.Add(0); data.Add(12); // PTR
            data.Add(0); data.Add(1);  // IN

            var packet = data.ToArray();
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4), 1); // 1 question

            // Act
            var success = DnsPacketParser.TryParse(packet, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void Should_Handle_Cache_Flush_Bit_In_Class()
        {
            // Arrange - Build a query with cache flush bit set (0x8001)
            var packet = BuildQueryPacketWithClass("test.local", MdnsConstants.TypeA, MdnsConstants.ClassINFlush);
            DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert - The class should be normalized to IN (1)
            Assert.Single(packetInfo.Questions);
            Assert.Equal(MdnsConstants.ClassIN, packetInfo.Questions[0].Class);
        }

        [Fact]
        public void Should_Parse_Real_World_Mdns_Query()
        {
            // Arrange - A real mDNS query packet structure
            var packet = BuildRealWorldPtrQuery("_services._dns-sd._udp.local");

            // Act
            var success = DnsPacketParser.TryParse(packet, out var packetInfo);

            // Assert
            Assert.True(success);
            Assert.True(packetInfo.IsQuery);
            Assert.Single(packetInfo.Questions);
            Assert.Equal("_services._dns-sd._udp.local", packetInfo.Questions[0].Name);
            Assert.Equal(MdnsConstants.TypePTR, packetInfo.Questions[0].Type);
        }

        #region Helper Methods

        private static byte[] BuildPtrQueryPacket(string name)
        {
            return BuildQueryPacket(name, MdnsConstants.TypePTR);
        }

        private static byte[] BuildQueryPacket(string name, ushort type)
        {
            return BuildQueryPacketWithClass(name, type, MdnsConstants.ClassIN);
        }

        private static byte[] BuildQueryPacketWithClass(string name, ushort type, ushort @class)
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

            // Class
            var classBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(classBytes, @class);
            data.AddRange(classBytes);

            return data.ToArray();
        }

        private static byte[] BuildMultiQuestionPacket((string name, ushort type)[] questions)
        {
            var data = new List<byte>();

            // Header
            data.AddRange(new byte[] { 0, 0 }); // Transaction ID
            data.AddRange(new byte[] { 0, 0 }); // Flags (query)

            var questionCountBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(questionCountBytes, (ushort)questions.Length);
            data.AddRange(questionCountBytes);

            data.AddRange(new byte[] { 0, 0 }); // Answer count
            data.AddRange(new byte[] { 0, 0 }); // Authority count
            data.AddRange(new byte[] { 0, 0 }); // Additional count

            // Questions
            foreach (var (name, type) in questions)
            {
                data.AddRange(DnsPacketBuilder.EncodeDnsName(name));

                var typeBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(typeBytes, type);
                data.AddRange(typeBytes);

                var classBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(classBytes, MdnsConstants.ClassIN);
                data.AddRange(classBytes);
            }

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

        private static byte[] BuildRealWorldPtrQuery(string name)
        {
            // Build a standard mDNS query packet
            var data = new List<byte>();

            // Header
            data.Add(0); data.Add(0);       // Transaction ID = 0 (mDNS)
            data.Add(0); data.Add(0);       // Flags (standard query)
            data.Add(0); data.Add(1);       // Question count = 1
            data.Add(0); data.Add(0);       // Answer count = 0
            data.Add(0); data.Add(0);       // Authority count = 0
            data.Add(0); data.Add(0);       // Additional count = 0

            // Question section
            data.AddRange(DnsPacketBuilder.EncodeDnsName(name));

            // Type = PTR (12)
            data.Add(0); data.Add(12);

            // Class = IN (1) with unicast response bit (0x8001 for QU)
            data.Add(0); data.Add(1);

            return data.ToArray();
        }

        #endregion
    }
}
