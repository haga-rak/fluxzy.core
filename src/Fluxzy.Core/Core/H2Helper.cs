// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Core
{
    public static class H2Helper
    {
        public static byte[] SettingAckBuffer { get; }

        static H2Helper()
        {
            var settingFrame = new SettingFrame(true);
            SettingAckBuffer = new byte[9];
            settingFrame.Write(SettingAckBuffer);
        }

        /// <summary>
        /// Should return true if an ACK frame is need to be sent
        /// </summary>
        /// <param name="streamSetting"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool ProcessSettingFrame(
            H2StreamSetting streamSetting, H2FrameReadResult frame)
        {
            var indexer = 0;
            var sendAck = false;

            while (frame.TryReadNextSetting(out var settingFrame, ref indexer)) {
                var needAck = ProcessIncomingSettingFrame(streamSetting, ref settingFrame);
                sendAck = sendAck || needAck;
            }

            if (sendAck) {
                return true;
            }

            return false;
        }

        public static bool ProcessIncomingSettingFrame(H2StreamSetting setting, ref SettingFrame settingFrame)
        {
            if (settingFrame.Ack)
                return false;

            switch (settingFrame.SettingIdentifier) {
                case SettingIdentifier.SettingsEnablePush:
                    if (settingFrame.Value > 0)

                        // TODO Send a Goaway. Push not supported 
                        return false;

                    return true;

                case SettingIdentifier.SettingsMaxConcurrentStreams:
                    setting.Remote.SettingsMaxConcurrentStreams = settingFrame.Value;

                    return true;

                case SettingIdentifier.SettingsInitialWindowSize:
                    setting.OverallWindowSize = settingFrame.Value;

                    return true;

                case SettingIdentifier.SettingsMaxFrameSize:
                    setting.Remote.MaxFrameSize = settingFrame.Value;

                    return true;

                case SettingIdentifier.SettingsMaxHeaderListSize:
                    setting.Remote.MaxHeaderListSize = settingFrame.Value;

                    return true;

                case SettingIdentifier.SettingsHeaderTableSize:
                    setting.SettingsHeaderTableSize = settingFrame.Value;

                    return true;
            }

            // We do not throw anything here, some server  
            // sends an identifier equals to 8 that match none of the value of rfc 7540

            // ---> old : throw new InvalidOperationException("Unknown setting type");

            return false;
        }

        internal static Memory<char> DecodeAndAllocate(IHeaderEncoder headerEncoder, ReadOnlySpan<byte> onWire)
        {
            var byteArray = ArrayPool<char>.Shared.Rent(1024 * 64);

            try
            {
                Span<char> tempBuffer = byteArray;

                var decoded = headerEncoder.Decoder.Decode(onWire, tempBuffer);
                Memory<char> charBuffer = new char[decoded.Length + 256];

                decoded.CopyTo(charBuffer.Span);
                var length = decoded.Length;

                return charBuffer.Slice(0, length);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(byteArray);
            }
        }
    }
}
