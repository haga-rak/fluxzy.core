// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Represents a parsed DNS question.
    /// </summary>
    internal readonly struct DnsQuestion
    {
        public DnsQuestion(string name, ushort type, ushort @class)
        {
            Name = name;
            Type = type;
            Class = @class;
        }

        /// <summary>
        /// The question name (e.g., "_fluxzyproxy._tcp.local").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The question type (e.g., PTR, A, SRV).
        /// </summary>
        public ushort Type { get; }

        /// <summary>
        /// The question class (usually IN).
        /// </summary>
        public ushort Class { get; }
    }

    /// <summary>
    /// Represents a parsed DNS packet header and questions.
    /// </summary>
    internal readonly struct DnsPacketInfo
    {
        public DnsPacketInfo(
            ushort transactionId,
            ushort flags,
            IReadOnlyList<DnsQuestion> questions)
        {
            TransactionId = transactionId;
            Flags = flags;
            Questions = questions;
        }

        /// <summary>
        /// Transaction ID (usually 0 for mDNS).
        /// </summary>
        public ushort TransactionId { get; }

        /// <summary>
        /// DNS flags field.
        /// </summary>
        public ushort Flags { get; }

        /// <summary>
        /// List of questions in the packet.
        /// </summary>
        public IReadOnlyList<DnsQuestion> Questions { get; }

        /// <summary>
        /// Returns true if this is a query (QR bit = 0).
        /// </summary>
        public bool IsQuery => (Flags & 0x8000) == 0;

        /// <summary>
        /// Returns true if this is a response (QR bit = 1).
        /// </summary>
        public bool IsResponse => (Flags & 0x8000) != 0;
    }

    /// <summary>
    /// Parses DNS packets for mDNS query detection.
    /// </summary>
    internal static class DnsPacketParser
    {
        private const int MinHeaderSize = 12;

        /// <summary>
        /// Tries to parse a DNS packet.
        /// </summary>
        /// <param name="data">The raw DNS packet data.</param>
        /// <param name="packetInfo">The parsed packet info if successful.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<byte> data, out DnsPacketInfo packetInfo)
        {
            packetInfo = default;

            if (data.Length < MinHeaderSize)
                return false;

            try
            {
                var transactionId = BinaryPrimitives.ReadUInt16BigEndian(data);
                var flags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2));
                var questionCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(4));
                // Skip answer, authority, additional counts for now

                var questions = new List<DnsQuestion>();
                var offset = MinHeaderSize;

                for (var i = 0; i < questionCount; i++)
                {
                    if (!TryParseName(data, ref offset, out var name))
                        return false;

                    if (offset + 4 > data.Length)
                        return false;

                    var type = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
                    var @class = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2));
                    offset += 4;

                    // Clear the cache flush bit from class for comparison
                    var classWithoutFlush = (ushort)(@class & 0x7FFF);

                    questions.Add(new DnsQuestion(name, type, classWithoutFlush));
                }

                packetInfo = new DnsPacketInfo(transactionId, flags, questions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the packet contains a PTR query for the specified service type.
        /// </summary>
        /// <param name="packetInfo">The parsed packet info.</param>
        /// <param name="serviceType">The service type to match (e.g., "_fluxzyproxy._tcp.local").</param>
        /// <returns>True if the packet is a PTR query for the specified service.</returns>
        public static bool IsPtrQueryForService(in DnsPacketInfo packetInfo, string serviceType)
        {
            if (!packetInfo.IsQuery)
                return false;

            foreach (var question in packetInfo.Questions)
            {
                if (question.Type == MdnsConstants.TypePTR &&
                    question.Class == MdnsConstants.ClassIN &&
                    string.Equals(question.Name, serviceType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the packet contains any query for the specified service type or instance.
        /// This includes PTR, SRV, TXT, and A record queries.
        /// </summary>
        /// <param name="packetInfo">The parsed packet info.</param>
        /// <param name="serviceType">The service type (e.g., "_fluxzyproxy._tcp.local").</param>
        /// <param name="instanceName">The full instance name (e.g., "MyProxy._fluxzyproxy._tcp.local").</param>
        /// <param name="hostFqdn">The host FQDN (e.g., "myhost.local").</param>
        /// <returns>True if the packet is a query for our service.</returns>
        public static bool IsQueryForService(
            in DnsPacketInfo packetInfo,
            string serviceType,
            string instanceName,
            string hostFqdn)
        {
            if (!packetInfo.IsQuery)
                return false;

            foreach (var question in packetInfo.Questions)
            {
                if (question.Class != MdnsConstants.ClassIN)
                    continue;

                // PTR query for service type
                if (question.Type == MdnsConstants.TypePTR &&
                    string.Equals(question.Name, serviceType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // SRV or TXT query for instance name
                if ((question.Type == MdnsConstants.TypeSRV || question.Type == MdnsConstants.TypeTXT) &&
                    string.Equals(question.Name, instanceName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // A query for host FQDN
                if (question.Type == MdnsConstants.TypeA &&
                    string.Equals(question.Name, hostFqdn, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to parse a DNS name from the packet.
        /// Handles both regular labels and compression pointers.
        /// </summary>
        private static bool TryParseName(ReadOnlySpan<byte> data, ref int offset, out string name)
        {
            name = string.Empty;
            var labels = new List<string>();
            var visited = new HashSet<int>();

            if (!TryParseNameInternal(data, ref offset, labels, visited, followPointers: true))
                return false;

            name = string.Join(".", labels);
            return true;
        }

        private static bool TryParseNameInternal(
            ReadOnlySpan<byte> data,
            ref int offset,
            List<string> labels,
            HashSet<int> visited,
            bool followPointers)
        {
            var movedOffset = false;

            // Must have at least one byte for the name (even if it's just the null terminator)
            if (offset >= data.Length)
                return false;

            while (offset < data.Length)
            {
                var length = data[offset];

                // Null terminator - end of name
                if (length == 0)
                {
                    if (!movedOffset)
                        offset++;
                    return true;
                }

                // Compression pointer (top 2 bits are 11)
                if ((length & 0xC0) == 0xC0)
                {
                    if (offset + 1 >= data.Length)
                        return false;

                    var pointer = ((length & 0x3F) << 8) | data[offset + 1];

                    // Prevent infinite loops
                    if (visited.Contains(pointer))
                        return false;

                    visited.Add(pointer);

                    if (!movedOffset)
                    {
                        offset += 2;
                        movedOffset = true;
                    }

                    var pointerOffset = pointer;
                    return TryParseNameInternal(data, ref pointerOffset, labels, visited, followPointers: true);
                }

                // Regular label
                if (length > 63)
                    return false;

                if (offset + 1 + length > data.Length)
                    return false;

                var label = Encoding.ASCII.GetString(data.Slice(offset + 1, length));
                labels.Add(label);
                offset += 1 + length;
            }

            return false;
        }

        /// <summary>
        /// Decodes a DNS name from raw bytes (for testing).
        /// </summary>
        public static string DecodeDnsName(ReadOnlySpan<byte> data)
        {
            var offset = 0;
            if (TryParseName(data, ref offset, out var name))
                return name;
            return string.Empty;
        }
    }
}
