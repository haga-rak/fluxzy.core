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

        public uint SettingsHeaderTableSize { get; set; } = 4096;


        /// <summary>
        /// Read buffer used by the connection. Should be at least MAX_FRAME_SIZE
        /// </summary>
        public int ReadBufferLength { get; set; } = 0x4000;


        public TimeSpan WaitForSettingDelay { get; set; } = TimeSpan.FromSeconds(3);
    }


    public class PeerSetting
    {
        public uint WindowSize { get; set; } = uint.MaxValue - 1;

        public uint MaxFrameSize { get; set; } = 0x4000;

        public bool EnablePush { get; set; } = false;

        public uint MaxHeaderListSize { get; set; } = 0x4000;

        public uint SettingsMaxConcurrentStreams { get; set; } = 100;

        public int MaxHeaderLine { get; set; } = 1024 * 4; 

        public int MaxHeaderSize { get; set; } = 1024 * 8; 
    }

}