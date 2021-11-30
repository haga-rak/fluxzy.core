// Copyright © 2021 Haga Rakotoharivelo

using System;

namespace Echoes.H2.Cli
{
    public class H2StreamSetting
    {
        public H2StreamSetting()
        {

        }

        public PeerSetting Local { get; set; } = new PeerSetting();

        public PeerSetting Remote { get; set; } = new PeerSetting();

        public int SettingsHeaderTableSize { get; set; } = 4096;

        public int MaxHeaderSize { get; set; } = 1024 * 8;


        /// <summary>
        /// Read buffer used by the connection. Should be at least MAX_FRAME_SIZE
        /// </summary>
        public int ReadBufferLength { get; set; } = 0x4000;


        public TimeSpan WaitForSettingDelay { get; set; } = TimeSpan.FromSeconds(3);
    }


    public class PeerSetting
    {
        public int WindowSize { get; set; } = int.MaxValue - 1;

        public int MaxFrameSize { get; set; } = 0x4000;

        public bool EnablePush { get; set; } = false;

        public int MaxHeaderListSize { get; set; } = 0x4000;

        public int SettingsMaxConcurrentStreams { get; set; } = 100;

        public int MaxHeaderLine { get; set; } = 1024 * 4; 

        public int MaxHeaderSize { get; set; } = 1024 * 8; 
    }

}