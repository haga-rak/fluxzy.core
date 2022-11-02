using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap;

internal static class FlagInterpreter
{
    public static bool IsUp(this PcapDevice device)
    {
        return (((LibPcapLiveDevice) device).Flags & 0x00000002) > 0; 
    }
    public static bool IsConnected(this PcapDevice device)
    {
        return (((LibPcapLiveDevice) device).Flags & 0x00000010) > 0; 
    }
    public static bool IsRunning(this PcapDevice device)
    {
        return (((LibPcapLiveDevice) device).Flags & 0x00000004) > 0; 
    }
        
    public static bool IsLoopback(this PcapDevice device)
    {
        return (((LibPcapLiveDevice) device).Flags & 0x00000001) > 0; 
    }
}