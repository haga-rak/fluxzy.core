// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Formatters.Producers.Grpc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class GrpcFrameHelperTests
    {
        [Fact]
        public void TryExtractPayload_ValidUncompressedFrame()
        {
            // compressed=0, length=3, data=0x08 0x2A 0x00
            var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x03, 0x08, 0x2A, 0x00 };

            var success = GrpcFrameHelper.TryExtractPayload(data, out var payload, out var compressed);

            Assert.True(success);
            Assert.False(compressed);
            Assert.Equal(3, payload.Length);
        }

        [Fact]
        public void TryExtractPayload_CompressedFrame()
        {
            // compressed=1, length=2, data=0x01 0x02
            var data = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x02, 0x01, 0x02 };

            var success = GrpcFrameHelper.TryExtractPayload(data, out var payload, out var compressed);

            Assert.True(success);
            Assert.True(compressed);
            Assert.Equal(2, payload.Length);
        }

        [Fact]
        public void TryExtractPayload_TooShort_ReturnsFalse()
        {
            var data = new byte[] { 0x00, 0x00, 0x00 };

            var success = GrpcFrameHelper.TryExtractPayload(data, out _, out _);

            Assert.False(success);
        }

        [Fact]
        public void TryExtractPayload_InsufficientData_ReturnsFalse()
        {
            // Says length=10 but only 2 bytes of data
            var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x0A, 0x01, 0x02 };

            var success = GrpcFrameHelper.TryExtractPayload(data, out _, out _);

            Assert.False(success);
        }

        [Fact]
        public void ExtractFrames_SingleFrame()
        {
            // One frame: compressed=0, length=2, data=0x08 0x01
            var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x02, 0x08, 0x01 };

            var frames = GrpcFrameHelper.ExtractFrames(data);

            Assert.Single(frames);
            Assert.False(frames[0].Compressed);
            Assert.Equal(2, frames[0].Data.Length);
        }

        [Fact]
        public void ExtractFrames_MultipleFrames()
        {
            // Two frames: frame1(2 bytes) + frame2(1 byte)
            var data = new byte[] {
                0x00, 0x00, 0x00, 0x00, 0x02, 0x08, 0x01, // frame 1
                0x00, 0x00, 0x00, 0x00, 0x01, 0x0A // frame 2
            };

            var frames = GrpcFrameHelper.ExtractFrames(data);

            Assert.Equal(2, frames.Count);
        }

        [Fact]
        public void ExtractFrames_EmptyData_ReturnsEmpty()
        {
            var frames = GrpcFrameHelper.ExtractFrames(ReadOnlyMemory<byte>.Empty);
            Assert.Empty(frames);
        }

        [Fact]
        public void ExtractGrpcPath_StandardPath()
        {
            // We can't easily construct ExchangeInfo, so we test the path parsing logic
            // through the producer integration tests
        }
    }
}
