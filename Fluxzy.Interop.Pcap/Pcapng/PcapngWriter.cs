using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Fluxzy.Interop.Pcap.Pcapng.Structs;
using SharpPcap;

namespace Fluxzy.Interop.Pcap.Pcapng
{
    public class PcapngGlobalInfo
    {
        public PcapngGlobalInfo(string userApplicationName, 
            string? osDescription = null, 
            string? hardwareDescription = null)
        {
            UserApplicationName = userApplicationName;
            OsDescription = osDescription;
            HardwareDescription = hardwareDescription;
        }

        public string UserApplicationName { get; }
        
        public string? OsDescription { get; }
        
        public string? HardwareDescription { get; }
    }

    /// <summary>
    /// This writer aims to dump packet into pcpang format with minimal GC pressure 
    /// </summary>
    public class PcapngWriter
    {
        private readonly string _userApplicationName;
        private readonly string _hardwareDescription;
        private readonly string _osDescription;

        private readonly ConcurrentDictionary<int, InterfaceDescription> _interfaces = new();

        private int _currentIndex = 0; 

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

        public void Write(Stream stream, PacketCapture capture)
        {
            var interfaceKey = capture.Device.MacAddress.GetHashCode();

            var interfaceDescription = _interfaces.GetOrAdd(interfaceKey, (key, info) =>
            {
                var description =  new InterfaceDescription(
                    (ushort) info.Device.LinkType, info.Existing.Count) {
                    Name = info.Device.Name,
                    Description = info.Device.Description
                };

                info.WriteAction(info.Stream, description);
                
                return description; 

            }, new InterfaceAddInfo(capture.Device, _interfaces, WriteInterfaceDescription, stream));

            var enhancedPacketBlock = new EnhancedPacketBlock(
                interfaceDescription.InterfaceId, 
                (uint) capture.Header.Timeval.Seconds,
                (uint) capture.Header.Timeval.MicroSeconds,
                capture.Data.Length,
                capture.Data.Length,
                "fxzy"
            );

            Span<byte> enhancedPacketBlockBuffer = stackalloc byte[enhancedPacketBlock.BlockTotalLength];

            var offset = enhancedPacketBlock.Write(enhancedPacketBlockBuffer, capture.Data);

            stream.Write(enhancedPacketBlockBuffer.Slice(0, offset));
        }

        public readonly struct InterfaceAddInfo
        {
            public InterfaceAddInfo(ICaptureDevice device, 
                    ConcurrentDictionary<int, 
                    InterfaceDescription> existing, 
                    Action<Stream, InterfaceDescription> writeAction, Stream stream)
            {
                Device = device;
                Existing = existing;
                WriteAction = writeAction;
                Stream = stream;
            }

            public ICaptureDevice Device { get;  }

            public ConcurrentDictionary<int, InterfaceDescription> Existing { get; }

            public Action<Stream, InterfaceDescription> WriteAction { get; }


            public Stream Stream { get; }
        }
    }
}
