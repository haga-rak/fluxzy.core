using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    internal static class SettingHelper
    {
        private static int WriteSetting(Memory<byte> buffer, PeerSetting setting, H2Logger logger)
        {
            var pushDisabled = new SettingFrame(SettingIdentifier.SettingsEnablePush, 0);

            int written = pushDisabled.Write(buffer.Span);

            var incrementUpdate = setting.WindowSize - 65535;

            logger.OutgoingSetting(ref pushDisabled);

            if (incrementUpdate != 0 )
            {
                var windowUpdateFrame = new WindowUpdateFrame(incrementUpdate, 0);

                //written += windowUpdateFrame.Write(buffer.Span.Slice(written));

                //logger.OutgoingWindowUpdate(incrementUpdate, 0);
            }

            return written;
        }

        public static async Task WriteSetting(Stream innerStream, PeerSetting setting, H2Logger logger, 
            CancellationToken token)
        {
            byte [] settingBuffer = new byte[80];

            var written = WriteSetting(settingBuffer, setting, logger);
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