// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    /// <summary>
    ///     We choose not to use a common interface for Http/2 frames to avoid boxing/unboxing.
    /// </summary>
    public readonly struct H2FrameReadResult
    {
        private readonly ReadOnlyMemory<byte> _bodyBytes;

        public H2FrameReadResult(H2Frame header, ReadOnlyMemory<byte> bodyBytes)
        {
            _bodyBytes = bodyBytes;
            BodyType = header.BodyType;
            StreamIdentifier = header.StreamIdentifier;
            Flags = header.Flags;
            BodyLength = header.BodyLength;
        }

        public bool IsEmpty => BodyLength == default && Flags == default && BodyType == default
                               && StreamIdentifier == 0;

        public int BodyLength { get; }

        public HeaderFlags Flags { get; }

        public int StreamIdentifier { get; }

        public H2FrameType BodyType { get; }

        public DataFrame GetDataFrame()
        {
            return new DataFrame(_bodyBytes, Flags, StreamIdentifier);
        }

        public bool TryReadNextSetting(out SettingFrame settingFrame, ref int index)
        {
            if (BodyLength == 0 && index == 0) {
                settingFrame = new SettingFrame(_bodyBytes.Span.Slice(index), Flags);
                index += 1;

                // ACK frame

                return true;
            }

            settingFrame = default;

            if (BodyLength - index < 4)
                return false;

            settingFrame = new SettingFrame(_bodyBytes.Span.Slice(index), Flags);
            index += settingFrame.BodyLength;

            return true;
        }

        public HeadersFrame GetHeadersFrame()
        {
            return new HeadersFrame(_bodyBytes, Flags);
        }

        public ContinuationFrame GetContinuationFrame()
        {
            return new ContinuationFrame(_bodyBytes, Flags, StreamIdentifier);
        }

        public RstStreamFrame GetRstStreamFrame()
        {
            return new RstStreamFrame(_bodyBytes.Span, StreamIdentifier);
        }

        public PingFrame GetPingFrame()
        {
            return new PingFrame(_bodyBytes.Span, Flags);
        }

        public PriorityFrame GetPriorityFrame()
        {
            return new PriorityFrame(_bodyBytes.Span, StreamIdentifier);
        }

        public GoAwayFrame GetGoAwayFrame()
        {
            return new GoAwayFrame(_bodyBytes.Span);
        }

        public WindowUpdateFrame GetWindowUpdateFrame()
        {
            return new WindowUpdateFrame(_bodyBytes.Span, StreamIdentifier);
        }

        public override string ToString()
        {
            return $"{BodyType} : streamId : {StreamIdentifier} : Length {BodyLength} : Flags : {Flags} ";
        }
    }
}
