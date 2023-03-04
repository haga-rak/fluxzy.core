// // Copyright 2022 - Haga Rakotoharivelo
// 

namespace Fluxzy.Interop.Pcap.Pcapng
{
    public class PcapngGlobalInfo
    {
        public PcapngGlobalInfo(
            string userApplicationName,
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
}
