using System;
using System.Buffers;
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

            return written;
        }

        public static async Task WriteSetting(Stream innerStream, PeerSetting setting, H2Logger logger, 
            CancellationToken token)
        {
            byte [] settingBuffer = ArrayPool<byte>.Shared.Rent(80);

            try
            {
                var written = WriteSetting(settingBuffer, setting, logger);
                await innerStream.WriteAsync(settingBuffer, 0, written, token);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(settingBuffer);
            }

        }

        public static async Task WriteAckSetting(Stream innerStream)
        {
            byte[] settingBuffer = ArrayPool<byte>.Shared.Rent(80);

            try
            {
                int written = new SettingFrame(true).Write(settingBuffer);
                await innerStream.WriteAsync(settingBuffer, 0, written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(settingBuffer);
            }
        }

        public static void WriteAck(Stream innerStream)
        {
            Span<byte> settingBuffer = stackalloc byte[80];
            int written = new SettingFrame(true).Write(settingBuffer);
            innerStream.Write(settingBuffer.Slice(0, written));

        }
    }
}