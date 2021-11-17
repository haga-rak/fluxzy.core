namespace Echoes.H2.Cli
{
    public  class StreamBufferSetting
    {
        public StreamBufferSetting() : this (1024 * 64, 1024 * 8)
        {

        }

        public StreamBufferSetting(int windowSizeBuffer, int headerBuffer)
        {
            WindowSizeBuffer = windowSizeBuffer;
            HeaderBuffer = headerBuffer;
        }

        /// <summary>
        /// Length of windows size buffer 
        /// </summary>
        public int WindowSizeBuffer { get; } 

        /// <summary>
        /// Length of buffer for header. Minimum recommended size is 8K
        /// </summary>
        public int HeaderBuffer { get;  }
    }
}