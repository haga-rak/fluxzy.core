using System.Runtime.InteropServices;
using Fluxzy.Interop.Pcap.Pcapng.Structs;

namespace Fluxzy.Interop.Pcap.Pcapng
{
    /// <summary>
    /// This writer aims to dump packet into pcpang format with minimal GC pressure 
    /// </summary>
    public class PcapngWriter
    {
        private readonly string _userApplicationName;
        private readonly string _hardwareDescription;
        private readonly string _osDescription;

        public PcapngWriter(string userApplicationName, string? osDescription = null, string? hardwareDescription = null)
        {
            _userApplicationName = userApplicationName;
            _hardwareDescription = hardwareDescription ?? RuntimeInformation.RuntimeIdentifier;
            _osDescription = osDescription ?? RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Static constant field
        /// </summary>
        public void WriteHeader(Stream stream)
        {
            // Don't be schoked by the generic interface, it's just to avoid unboxing 
            
            var userAppOption = new StringOptionBlock(OptionBlockCode.Shb_UserAppl, _userApplicationName);
            var osDecriptionOption = new StringOptionBlock(OptionBlockCode.Shb_Os, _osDescription);
            var hardwareOption = new StringOptionBlock(OptionBlockCode.Shb_Hardware, _hardwareDescription);
            var endOfOption = new EndOfOption();

            var sectionHeaderBlock = new SectionHeaderBlock(
                osDecriptionOption.OnWireLength
                + userAppOption.OnWireLength
                + hardwareOption.OnWireLength
                + endOfOption.OnWriteLength);

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
    }
}
