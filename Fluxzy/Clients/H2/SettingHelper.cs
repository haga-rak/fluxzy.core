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
        private static int WriteStartupSetting(Span<byte> buffer, PeerSetting setting, H2Logger logger)
        {
            var written = 0;

            {
                //var currentSetting = new SettingFrame(SettingIdentifier.SettingsEnablePush, 0);
                //written += currentSetting.Write(buffer);
                //logger.OutgoingSetting(ref currentSetting);
            }

            {
                //var currentSetting = new SettingFrame(SettingIdentifier.SettingsInitialWindowSize, 1073741824);
                //written += currentSetting.Write(buffer);
                //logger.OutgoingSetting(ref currentSetting);
            }

            {
                var currentSetting = new SettingFrame(SettingIdentifier.SettingsMaxConcurrentStreams, 256);
                written += currentSetting.Write(buffer);
                logger.OutgoingSetting(ref currentSetting);
            }

            return written;
        }

        public static void WriteWelcomeSettings(Stream innerStream, PeerSetting setting, H2Logger logger)
        {
            Span<byte> settingBuffer = stackalloc byte[128];

            var written = WriteStartupSetting(settingBuffer, setting, logger);
            innerStream.Write(settingBuffer[..written]);
        }

        public static async Task WriteAckSetting(Stream innerStream)
        {
            var settingBuffer = ArrayPool<byte>.Shared.Rent(80);

            try {
                var written = new SettingFrame(true).Write(settingBuffer);
                await innerStream.WriteAsync(settingBuffer, 0, written);
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
