using System.Runtime.InteropServices;
using Fluxzy.Interop.Pcap.Pcapng.Structs;
using SharpPcap;

namespace Fluxzy.Interop.Pcap.Pcapng
{
    /// <summary>
    /// This writer aims to dump packet into pcpang format with minimal GC pressure 
    /// </summary>
    public class PcapngStreamWriter
    {
        private readonly string _userApplicationName;
        private readonly string _hardwareDescription;
        private readonly string _osDescription;

        private readonly Dictionary<int, InterfaceDescription> _interfaces = new();
        
        public PcapngStreamWriter(PcapngGlobalInfo pcapngGlobalInfo)
        {
            _userApplicationName = pcapngGlobalInfo.UserApplicationName;
            _hardwareDescription = pcapngGlobalInfo.HardwareDescription ?? RuntimeInformation.RuntimeIdentifier;
            _osDescription = pcapngGlobalInfo.OsDescription ?? RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Static constant field
        /// </summary>
        public void WriteSectionHeaderBlock(Stream stream)
        {
            var userAppOption = new StringOptionBlock(OptionBlockCode.Shb_UserAppl, _userApplicationName);
            var osDecriptionOption = new StringOptionBlock(OptionBlockCode.Shb_Os, _osDescription);
            var hardwareOption = new StringOptionBlock(OptionBlockCode.Shb_Hardware, _hardwareDescription);
            var endOfOption = new EndOfOption();

            var sectionHeaderBlock = new SectionHeaderBlock(
                osDecriptionOption.OnWireLength
                + userAppOption.OnWireLength
                + hardwareOption.OnWireLength
                + endOfOption.OnWireLength);

            //var sectionHeaderBlock = new SectionHeaderBlock(0);

            Span<byte> sectionHeaderBlockBuffer = stackalloc byte[sectionHeaderBlock.OnWireLength];

            int offset = 0;

            offset += sectionHeaderBlock.WriteHeader(sectionHeaderBlockBuffer);

            offset += userAppOption.Write(sectionHeaderBlockBuffer.Slice(offset));
            offset += osDecriptionOption.Write(sectionHeaderBlockBuffer.Slice(offset));
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

        public void Write(Stream stream, PacketCapture capture)
        {
            var interfaceKey = capture.Device.MacAddress.GetHashCode();

            if (!_interfaces.TryGetValue(interfaceKey, out var description)) {

                _interfaces[interfaceKey] = description = new InterfaceDescription(
                    (ushort) capture.Device.LinkType, _interfaces.Count)
                {
                    Name = capture.Device.Name,
                    Description = capture.Device.Description
                };

                WriteInterfaceDescription(stream, description);
            }
            
            
            var enhancedPacketBlock = new EnhancedPacketBlock(
                description.InterfaceId, 
                (uint) capture.Header.Timeval.Seconds,
                (uint) capture.Header.Timeval.MicroSeconds,
                capture.Data.Length,
                capture.Data.Length,
                "fxzy"
            );

            // This need to be corrected if MTU is very large

            Span<byte> enhancedPacketBlockBuffer = stackalloc byte[enhancedPacketBlock.BlockTotalLength];
            var offset = enhancedPacketBlock.Write(enhancedPacketBlockBuffer, capture.Data);
            stream.Write(enhancedPacketBlockBuffer.Slice(0, offset));
        }
    }
}
