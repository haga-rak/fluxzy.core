namespace Echoes.H2.Cli
{
    public readonly struct H2FrameReadResult
    {
        public H2FrameReadResult(H2Frame header, IFixedSizeFrame payload)
        {
            Header = header;
            Payload = payload;
        }

        public H2Frame Header { get;  }

        public IFixedSizeFrame Payload { get;  }
    }
}