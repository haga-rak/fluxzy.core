// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Builds DNS packets for mDNS announcements.
    /// </summary>
    internal class DnsPacketBuilder
    {
        private readonly List<byte> _buffer = new();

        /// <summary>
        /// Gets the current packet data.
        /// </summary>
        public byte[] GetPacket() => _buffer.ToArray();

        /// <summary>
        /// Clears the buffer for reuse.
        /// </summary>
        public void Clear() => _buffer.Clear();

        /// <summary>
        /// Writes a 16-bit unsigned integer in network byte order (big-endian).
        /// </summary>
        public void WriteUInt16(ushort value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            _buffer.Add(bytes[0]);
            _buffer.Add(bytes[1]);
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer in network byte order (big-endian).
        /// </summary>
        public void WriteUInt32(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            _buffer.Add(bytes[0]);
            _buffer.Add(bytes[1]);
            _buffer.Add(bytes[2]);
            _buffer.Add(bytes[3]);
        }

        /// <summary>
        /// Writes raw bytes to the buffer.
        /// </summary>
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            foreach (var b in data)
                _buffer.Add(b);
        }

        /// <summary>
        /// Writes a single byte to the buffer.
        /// </summary>
        public void WriteByte(byte value) => _buffer.Add(value);

        /// <summary>
        /// Encodes a DNS name using length-prefixed labels.
        /// Example: "test.local" becomes [4]test[5]local[0]
        /// </summary>
        public static byte[] EncodeDnsName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new byte[] { 0 };

            var result = new List<byte>();
            var labels = name.Split('.');

            foreach (var label in labels)
            {
                if (label.Length > 63)
                    throw new ArgumentException($"DNS label exceeds 63 characters: {label}");

                var labelBytes = Encoding.ASCII.GetBytes(label);
                result.Add((byte)labelBytes.Length);
                result.AddRange(labelBytes);
            }

            result.Add(0); // Null terminator
            return result.ToArray();
        }

        /// <summary>
        /// Encodes a TXT record value, splitting into chunks if needed.
        /// DNS TXT records consist of length-prefixed strings (max 255 bytes each).
        /// </summary>
        public static byte[] EncodeTxtRecord(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new byte[] { 0 };

            var textBytes = Encoding.UTF8.GetBytes(text);
            var result = new List<byte>();

            var offset = 0;
            while (offset < textBytes.Length)
            {
                var chunkSize = Math.Min(MdnsConstants.MaxTxtStringLength, textBytes.Length - offset);
                result.Add((byte)chunkSize);

                for (var i = 0; i < chunkSize; i++)
                    result.Add(textBytes[offset + i]);

                offset += chunkSize;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Writes a DNS header for an mDNS response.
        /// </summary>
        public void WriteHeader(ushort answerCount)
        {
            WriteUInt16(0);                           // Transaction ID (0 for mDNS)
            WriteUInt16(MdnsConstants.ResponseFlags); // Flags: QR=1, AA=1
            WriteUInt16(0);                           // Questions
            WriteUInt16(answerCount);                 // Answers
            WriteUInt16(0);                           // Authority RRs
            WriteUInt16(0);                           // Additional RRs
        }

        /// <summary>
        /// Writes a PTR record (service type → instance name).
        /// </summary>
        public void WritePtrRecord(string serviceType, string instanceName, uint ttl)
        {
            var nameBytes = EncodeDnsName(serviceType);
            var targetBytes = EncodeDnsName(instanceName);

            WriteBytes(nameBytes);
            WriteUInt16(MdnsConstants.TypePTR);
            WriteUInt16(MdnsConstants.ClassIN);
            WriteUInt32(ttl);
            WriteUInt16((ushort)targetBytes.Length);
            WriteBytes(targetBytes);
        }

        /// <summary>
        /// Writes an SRV record (instance → host:port).
        /// </summary>
        public void WriteSrvRecord(string instanceName, string hostname, ushort port, uint ttl)
        {
            var nameBytes = EncodeDnsName(instanceName);
            var targetBytes = EncodeDnsName(hostname);

            // SRV RDATA: priority (2) + weight (2) + port (2) + target
            var rdataLength = 2 + 2 + 2 + targetBytes.Length;

            WriteBytes(nameBytes);
            WriteUInt16(MdnsConstants.TypeSRV);
            WriteUInt16(MdnsConstants.ClassINFlush);
            WriteUInt32(ttl);
            WriteUInt16((ushort)rdataLength);
            WriteUInt16(0);                    // Priority
            WriteUInt16(0);                    // Weight
            WriteUInt16(port);                 // Port
            WriteBytes(targetBytes);           // Target hostname
        }

        /// <summary>
        /// Writes a TXT record with the given data.
        /// </summary>
        public void WriteTxtRecord(string instanceName, string txtData, uint ttl)
        {
            var nameBytes = EncodeDnsName(instanceName);
            var txtBytes = EncodeTxtRecord(txtData);

            WriteBytes(nameBytes);
            WriteUInt16(MdnsConstants.TypeTXT);
            WriteUInt16(MdnsConstants.ClassINFlush);
            WriteUInt32(ttl);
            WriteUInt16((ushort)txtBytes.Length);
            WriteBytes(txtBytes);
        }

        /// <summary>
        /// Writes an A record (hostname → IP address).
        /// </summary>
        public void WriteARecord(string hostname, IPAddress ipAddress, uint ttl)
        {
            var nameBytes = EncodeDnsName(hostname);
            var addressBytes = ipAddress.GetAddressBytes();

            if (addressBytes.Length != 4)
                throw new ArgumentException("Only IPv4 addresses are supported", nameof(ipAddress));

            WriteBytes(nameBytes);
            WriteUInt16(MdnsConstants.TypeA);
            WriteUInt16(MdnsConstants.ClassINFlush);
            WriteUInt32(ttl);
            WriteUInt16(4);                    // RDLENGTH for IPv4
            WriteBytes(addressBytes);
        }

        /// <summary>
        /// Builds a complete mDNS announcement packet.
        /// </summary>
        public static byte[] BuildAnnouncementPacket(
            string serviceName,
            string hostname,
            IPAddress ipAddress,
            ushort port,
            string txtData,
            uint ttl = MdnsConstants.DefaultTtl)
        {
            var builder = new DnsPacketBuilder();

            var serviceType = MdnsConstants.ServiceType;
            var instanceName = $"{serviceName}.{serviceType}";
            var hostFqdn = $"{hostname}.{MdnsConstants.Domain}";

            // Header with 4 answer records
            builder.WriteHeader(4);

            // PTR record: _fluxzyproxy._tcp.local → instance._fluxzyproxy._tcp.local
            builder.WritePtrRecord(serviceType, instanceName, ttl);

            // SRV record: instance._fluxzyproxy._tcp.local → hostname.local:port
            builder.WriteSrvRecord(instanceName, hostFqdn, port, ttl);

            // TXT record: instance._fluxzyproxy._tcp.local → metadata
            builder.WriteTxtRecord(instanceName, txtData, ttl);

            // A record: hostname.local → IP address
            builder.WriteARecord(hostFqdn, ipAddress, ttl);

            return builder.GetPacket();
        }

        /// <summary>
        /// Builds a goodbye packet (announcement with TTL=0).
        /// </summary>
        public static byte[] BuildGoodbyePacket(
            string serviceName,
            string hostname,
            IPAddress ipAddress,
            ushort port,
            string txtData)
        {
            return BuildAnnouncementPacket(serviceName, hostname, ipAddress, port, txtData, ttl: 0);
        }
    }
}
