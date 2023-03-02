using System.Runtime.InteropServices;
using Fluxzy.Interop.Pcap.Pcapng.Structs;

namespace Fluxzy.Interop.Pcap.Pcapng
{
    public class PcapngGlobalInfo
    {
        public PcapngGlobalInfo(string userApplicationName, string? osDescription = null, string? hardwareDescription = null)
        {
            UserApplicationName = userApplicationName;
            OsDescription = osDescription;
            HardwareDescription = hardwareDescription;
        }

        public string UserApplicationName { get; private set; }
        
        public string? OsDescription { get; private set; }
        
        public string? HardwareDescription { get; private set; }
    }

    /// <summary>
    /// This writer aims to dump packet into pcpang format with minimal GC pressure 
    /// </summary>
    public class PcapngWriter
    {
        private readonly string _userApplicationName;
        private readonly string _hardwareDescription;
        private readonly string _osDescription;

        public PcapngWriter(PcapngGlobalInfo pcapngGlobalInfo)
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
        
        public void WriteInterfaceDescription(Stream stream, InterfaceDescription interfaceDescription)
        {

            var interfaceDescriptionBlock = new InterfaceDescriptionBlock(interfaceDescription);
            

            Span<byte> interfaceDescriptionBlockBuffer = stackalloc byte[interfaceDescriptionBlock.BlockTotalLength];
            var offset = interfaceDescriptionBlock.Write(interfaceDescriptionBlockBuffer);

            stream.Write(interfaceDescriptionBlockBuffer.Slice(0, offset));


            // Write things about interface description
        }
    }
}
