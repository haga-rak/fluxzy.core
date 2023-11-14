using System.Text;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal static class OptionHelper
    {
        public static int GetOnWireLength(string str)
        {
            var optionLength = Encoding.UTF8.GetByteCount(str); 
            return 4 + optionLength + ((4 - optionLength % 4) % 4);
        }
    }
}