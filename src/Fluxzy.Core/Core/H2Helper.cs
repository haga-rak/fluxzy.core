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
            H2StreamSetting streamSetting, H2FrameReadResult frame, out H2ErrorCode? fatalError)
        {
            var indexer = 0;
            var isAckFrame = false;
            fatalError = null;

            while (frame.TryReadNextSetting(out var settingFrame, ref indexer)) {
                if (settingFrame.Ack) {
                    isAckFrame = true;
                }
                else {
                    ProcessIncomingSettingFrame(streamSetting, ref settingFrame);

                    if (settingFrame.SettingIdentifier == SettingIdentifier.SettingsEnablePush
                        && settingFrame.Value > 0) {
                        fatalError = H2ErrorCode.ProtocolError;
                    }
                }
            }

            // Per RFC 7540 Section 6.5: always ACK non-ACK SETTINGS frames
            return !isAckFrame;
        }

        public static bool ProcessIncomingSettingFrame(H2StreamSetting setting, ref SettingFrame settingFrame)
        {
            if (settingFrame.Ack)
                return false;

            switch (settingFrame.SettingIdentifier) {
                case SettingIdentifier.SettingsEnablePush:
                    return settingFrame.Value == 0;

                case SettingIdentifier.SettingsMaxConcurrentStreams:
                    setting.Remote.SettingsMaxConcurrentStreams = settingFrame.Value;

                    return true;

                case SettingIdentifier.SettingsInitialWindowSize:
                    setting.Remote.WindowSize = settingFrame.Value;

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

        private const int DecodeInitialBufferSize = 1024 * 64;
        private const int DecodeMaxBufferSize = 1024 * 1024;

        internal static Memory<char> DecodeAndAllocate(IHeaderEncoder headerEncoder, ReadOnlySpan<byte> onWire)
        {
            var bufferSize = DecodeInitialBufferSize;

            while (bufferSize <= DecodeMaxBufferSize)
            {
                var byteArray = ArrayPool<char>.Shared.Rent(bufferSize);

                try
                {
                    Span<char> tempBuffer = byteArray;

                    var decoded = headerEncoder.Decoder.Decode(onWire, tempBuffer);
                    Memory<char> charBuffer = new char[decoded.Length + 256];

                    decoded.CopyTo(charBuffer.Span);

                    return charBuffer.Slice(0, decoded.Length);
                }
                catch (IndexOutOfRangeException) when (bufferSize < DecodeMaxBufferSize)
                {
                    // Buffer too small for decoded headers, grow and retry
                    bufferSize *= 2;
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(byteArray);
                }
            }

            throw new FluxzyException(
                $"Decoded header size exceeds maximum allowed ({DecodeMaxBufferSize} chars)");
        }
    }
}
