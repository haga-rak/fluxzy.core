// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    internal static class SettingHelper
    {
        private static int WriteStartupSetting(Span<byte> buffer, H2StreamSetting h2Setting, H2Logger logger)
        {
            var written = 0;
            var headerCount = 9; 

            // 5 bytes header

            var totalSettingCount = 0;

            foreach (var (settingIdentifier, value) in h2Setting.GetAnnouncementSettings()) {

                var currentSetting = new SettingFrame(settingIdentifier, value);
                written += SettingFrame.WriteMultipleBody(buffer.Slice(written + headerCount), settingIdentifier, value);
                totalSettingCount++;
                logger.OutgoingSetting(ref currentSetting);
            }

            // 5 bytes header

            written += SettingFrame.WriteMultipleHeader(buffer, totalSettingCount);
            return written;
        }

        public static void WriteWelcomeSettings(byte [] preface, Stream innerStream, H2StreamSetting h2Setting, H2Logger logger)
        {
            Span<byte> settingBuffer = stackalloc byte[512];

            var written = 0;

            preface.AsSpan().CopyTo(settingBuffer);
            written += preface.Length;
            written += WriteStartupSetting(settingBuffer.Slice(written), h2Setting, logger);

            var windowSizeAnnounced = h2Setting.Local.WindowSize - 65535;

            if (windowSizeAnnounced != 0) {
                var windowFrame = new WindowUpdateFrame(windowSizeAnnounced, 0);
                written += windowFrame.Write(settingBuffer.Slice(written));
            }

            innerStream.Write(settingBuffer[..written]);
        }

        public static async Task WriteAckSetting(Stream innerStream)
        {
            var settingBuffer = ArrayPool<byte>.Shared.Rent(80);

            try {
                var written = new SettingFrame(true).Write(settingBuffer);
                await innerStream.WriteAsync(settingBuffer, 0, written).ConfigureAwait(false);
            }
            finally {
                ArrayPool<byte>.Shared.Return(settingBuffer);
            }
        }

        public static void WriteAck(Stream innerStream, H2Logger logger)
        {
            Span<byte> settingBuffer = stackalloc byte[80];
            var settingFrame = new SettingFrame(true);
            var written = settingFrame.Write(settingBuffer);

            logger.OutgoingSetting(ref settingFrame);
            innerStream.Write(settingBuffer.Slice(0, written));
        }
    }
}
