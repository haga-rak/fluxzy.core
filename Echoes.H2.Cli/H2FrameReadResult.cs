using System;

namespace Echoes.H2.Cli
{
    /// <summary>
    /// We choose not to use a common interface for Http/2 frames to avoid boxing/unboxing. 
    /// </summary>
    public readonly ref struct H2FrameReadResult
    {
        private readonly ReadOnlyMemory<byte> _bodyBytes;

        public H2FrameReadResult(H2Frame header, ReadOnlyMemory<byte> bodyBytes)
        {
            _bodyBytes = bodyBytes;
            Header = header;
            BodyType = header.BodyType;
        }

        public H2FrameType BodyType { get;  }

        public H2Frame Header { get;  }

        public DataFrame GetDataFrame()
        {
            return new DataFrame(_bodyBytes, Header.Flags, Header.StreamIdentifier);
        }

        public SettingFrame GetSettingFrame()
        {
            return new SettingFrame(_bodyBytes.Span, Header.Flags);
        }

        public HeaderFrame GetHeaderFrame()
        {
            return new HeaderFrame(_bodyBytes, Header.Flags);
        }

        public ContinuationFrame GetContinuationFrame()
        {
            return new ContinuationFrame(_bodyBytes, Header.Flags, Header.StreamIdentifier); 
        }

        public RstStreamFrame GetRstStreamFrame()
        {
            return new RstStreamFrame(_bodyBytes.Span,  Header.StreamIdentifier); 
        }

        public PriorityFrame GetPriorityFrame()
        {
            return new PriorityFrame(_bodyBytes.Span,  Header.StreamIdentifier); 
        }

        public GoAwayFrame GetGoAwayFrame()
        {
            return new GoAwayFrame(_bodyBytes.Span); 
        }

        public WindowUpdateFrame GetWindowUpdateFrame()
        {
            return new WindowUpdateFrame(_bodyBytes.Span); 
        }
    }
}