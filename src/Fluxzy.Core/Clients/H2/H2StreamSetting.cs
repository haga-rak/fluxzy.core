// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    public class H2StreamSetting
    {
        public PeerSetting Local { get; set; } = new() {
            WindowSize = 6291456, // 512Ko - 15663105 - 15 728 640
            SettingsMaxConcurrentStreams = 256
        };

        public PeerSetting Remote { get; set; } = new();

        public int SettingsHeaderTableSize { get; set; } = 65536;

        public int OverallWindowSize { get; set; } = 65536;

        public int MaxFrameSizeAllowed { get; set; } = 128 * 1024;

        public int MaxHeaderSize { get; set; } = 16384;

        /// <summary>
        ///     Number of idle seconds before the h2 connection is released
        /// </summary>
        public int MaxIdleSeconds { get; set; } = 500;

        /// <summary>
        ///     Read buffer used by the connection. Should be at least MAX_FRAME_SIZE
        /// </summary>
        public int ReadBufferLength { get; set; } = 0x4000;

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan WaitForSettingDelay { get; set; } = TimeSpan.FromSeconds(30);


        public HashSet<SettingIdentifier> AdvertiseSettings { get; set; } = new HashSet<SettingIdentifier>() {
            SettingIdentifier.SettingsHeaderTableSize,
            SettingIdentifier.SettingsEnablePush,
            SettingIdentifier.SettingsInitialWindowSize,
            SettingIdentifier.SettingsMaxHeaderListSize,
        };

        public IEnumerable<(SettingIdentifier SettingIdentifier, int Value)> GetAnnouncementSettings()
        {
            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsHeaderTableSize))
                yield return (SettingIdentifier.SettingsHeaderTableSize, SettingsHeaderTableSize);

            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsEnablePush))
                yield return (SettingIdentifier.SettingsEnablePush, Local.EnablePush ? 1 : 0);

            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsMaxConcurrentStreams))
                yield return (SettingIdentifier.SettingsMaxConcurrentStreams, Local.SettingsMaxConcurrentStreams);

            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsInitialWindowSize))
                yield return (SettingIdentifier.SettingsInitialWindowSize, Local.WindowSize);

            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsMaxFrameSize))
                yield return (SettingIdentifier.SettingsMaxFrameSize, Local.MaxFrameSize);

            if (AdvertiseSettings.Contains(SettingIdentifier.SettingsMaxHeaderListSize))
                yield return (SettingIdentifier.SettingsMaxHeaderListSize, Local.MaxHeaderListSize);
        }

        public void SetSetting(SettingIdentifier identifier, int value)
        {
            switch (identifier)
            {
                case SettingIdentifier.SettingsHeaderTableSize:
                    SettingsHeaderTableSize = value;
                    break;
                case SettingIdentifier.SettingsEnablePush:
                    Local.EnablePush = value == 1;
                    break;
                case SettingIdentifier.SettingsMaxConcurrentStreams:
                    Local.SettingsMaxConcurrentStreams = value;
                    break;
                case SettingIdentifier.SettingsInitialWindowSize:
                    Local.WindowSize = value;
                    break;
                case SettingIdentifier.SettingsMaxFrameSize:
                    Local.MaxFrameSize = value;
                    break;
                case SettingIdentifier.SettingsMaxHeaderListSize:
                    Local.MaxHeaderListSize = value;
                    break;
            }
        }
    }

    public class PeerSetting
    {
        public static PeerSetting Default { get; } = new();

        public int WindowSize { get; set; } = 6291456;

        public int MaxFrameSize { get; set; } = 0x4000;

        public bool EnablePush { get; set; } = false;

        public int MaxHeaderListSize { get; set; } = 262144;

        public int SettingsMaxConcurrentStreams { get; set; } = 100;

        public int MaxHeaderLine { get; set; } = 1024 * 16;
    }
}
