using System.Runtime.InteropServices;
using Fluxzy.Core.Pcap.Pcapng.Structs;
using Fluxzy.Misc.Streams;
using SharpPcap;

namespace Fluxzy.Core.Pcap.Pcapng
{
    /// <summary>
    ///     This writer aims to dump packet into pcpang format with minimal GC pressure
    /// </summary>
    public class PcapngStreamWriter
    {
        private static readonly DateTime Reference = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly string _hardwareDescription;

        private readonly Dictionary<int, InterfaceDescription> _interfaces = new();
        private readonly string _osDescription;

        private readonly string _userApplicationName;

        public PcapngStreamWriter(PcapngGlobalInfo pcapngGlobalInfo)
        {
            _userApplicationName = pcapngGlobalInfo.UserApplicationName;
            _hardwareDescription = "Unavailable on this runtime";
            
#if NET7_0_OR_GREATER
            _hardwareDescription = pcapngGlobalInfo.HardwareDescription ?? RuntimeInformation.RuntimeIdentifier;
#endif

            _osDescription = pcapngGlobalInfo.OsDescription ?? RuntimeInformation.OSDescription;
        }

        /// <summary>
        ///     Static constant field
        /// </summary>
        public void WriteSectionHeaderBlock(Stream stream)
        {
            var userAppOption = new StringOptionBlock(OptionBlockCode.Shb_UserAppl, _userApplicationName);
            var osDescription = new StringOptionBlock(OptionBlockCode.Shb_Os, _osDescription);
            var hardwareOption = new StringOptionBlock(OptionBlockCode.Shb_Hardware, _hardwareDescription);
            var endOfOption = new EndOfOption();

            var sectionHeaderBlock = new SectionHeaderBlock(
                osDescription.OnWireLength
                + userAppOption.OnWireLength
                + hardwareOption.OnWireLength
                + endOfOption.OnWireLength);
            
            Span<byte> sectionHeaderBlockBuffer = stackalloc byte[sectionHeaderBlock.OnWireLength];

            var offset = 0;

            offset += sectionHeaderBlock.WriteHeader(sectionHeaderBlockBuffer);

            offset += userAppOption.Write(sectionHeaderBlockBuffer.Slice(offset));
            offset += osDescription.Write(sectionHeaderBlockBuffer.Slice(offset));
            offset += hardwareOption.Write(sectionHeaderBlockBuffer.Slice(offset));

            offset += endOfOption.Write(sectionHeaderBlockBuffer.Slice(offset));

            offset += sectionHeaderBlock.WriteTrailer(sectionHeaderBlockBuffer.Slice(offset));

            stream.Write(sectionHeaderBlockBuffer.Slice(0, offset));
        }

        protected void WriteInterfaceDescription(Stream stream, InterfaceDescription interfaceDescription)
        {
            var interfaceDescriptionBlock = new InterfaceDescriptionBlock(interfaceDescription);

            Span<byte> interfaceDescriptionBlockBuffer = stackalloc byte[interfaceDescriptionBlock.BlockTotalLength];
            var offset = interfaceDescriptionBlock.Write(interfaceDescriptionBlockBuffer);

            stream.Write(interfaceDescriptionBlockBuffer.Slice(0, offset));

            // Write things about interface description
        }

        public void Write(Stream stream, ref PacketCapture capture)
        {
            var interfaceKey = capture.Device.MacAddress.GetHashCode();

            if (!_interfaces.TryGetValue(interfaceKey, out var description)) {
                _interfaces[interfaceKey] = description = new InterfaceDescription(
                    (ushort) capture.Device.LinkType, _interfaces.Count) {
                    Name = capture.Device.Name,
                    Description = capture.Device.Description,
                    MacAddress = capture.Device.MacAddress?.GetAddressBytes()
                };

                WriteInterfaceDescription(stream, description);
            }

            // var longTimeSpan = (capture.Header.Timeval.Date - Reference).Ticks / (100);
            var longTimeSpan = (long) ((capture.Header.Timeval.Date - Reference).TotalMilliseconds * 1000);

            var enhancedPacketBlock = new EnhancedPacketBlock(
                description.InterfaceId,
                (uint) (longTimeSpan >> 32),
                (uint) (longTimeSpan & 0xFFFFFFFF),
                capture.Data.Length,
                capture.Data.Length,
                Environment.GetEnvironmentVariable("FLUXZY_PCAP_PACKET_COMMENT") ?? "fluxzy"
            );

            // This need to be corrected if MTU is very large
            Span<byte> enhancedPacketBlockBuffer = stackalloc byte[enhancedPacketBlock.BlockTotalLength];
            var offset = enhancedPacketBlock.Write(enhancedPacketBlockBuffer, capture.Data);
            stream.Write(enhancedPacketBlockBuffer.Slice(0, offset));
        }

        public void WriteNssKey(Stream stream, string nssKeys)
        {
            var decryptionBlock = new NssDecryptionSecretsBlock(nssKeys);

            Span<byte> decryptionBlockBuffer = stackalloc byte[decryptionBlock.BlockTotalLength];
            var offset = decryptionBlock.Write(decryptionBlockBuffer, nssKeys);

            stream.Write(decryptionBlockBuffer);
        }

        public void WriteNssKey(Stream stream, Stream source)
        {
            var nssKey = source.ReadToEndGreedy();
            WriteNssKey(stream, nssKey);
        }
    }
}
