using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    internal static class SettingHelper
    {
        public static async Task WriteSetting(Stream innerStream, PeerSetting setting, CancellationToken token)
        {
            byte [] settingBuffer = new byte[16];

            int written = new SettingFrame(SettingIdentifier.SettingsEnablePush, 0).Write(settingBuffer);
            await innerStream.WriteAsync(settingBuffer, 0, written, token);
        }

        public static async Task WriteAckSetting(Stream innerStream)
        {
            byte[] settingBuffer = new byte[16]; 
            int written = new SettingFrame(true).Write(settingBuffer);

            await innerStream.WriteAsync(settingBuffer, 0, written);

        }
    }
}