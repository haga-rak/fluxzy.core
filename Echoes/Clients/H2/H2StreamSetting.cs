// Copyright © 2021 Haga Rakotoharivelo

using System;

namespace Echoes.H2
{
    public class H2StreamSetting
    {
        public PeerSetting Local { get; set; } = new PeerSetting()
        {
            WindowSize = 1024 * 1024 * 2// 512Ko
        };

        public PeerSetting Remote { get; set; } = new PeerSetting();

        public int SettingsHeaderTableSize { get; set; } = 4096;

        public int OverallWindowSize { get; set; } = 65536; 

        public int MaxHeaderSize { get; set; } = 16384;

        /// <summary>
        /// Read buffer used by the connection. Should be at least MAX_FRAME_SIZE
        /// </summary>
        public int ReadBufferLength { get; set; } = 0x4000;

        public TimeSpan WaitForSettingDelay { get; set; } = TimeSpan.FromSeconds(30);
    }


    public class PeerSetting
    {
        public static PeerSetting Default { get; } = new PeerSetting(); 

        public int WindowSize { get; set; } = 0XFFFF;

        public int MaxFrameSize { get; set; } = 0x4000;

        public bool EnablePush { get;  } = false;

        public int MaxHeaderListSize { get; set; } = 0x4000;

        public int SettingsMaxConcurrentStreams { get; set; } = 100;

        public int MaxHeaderLine { get; set; } = 16384;
    }

}