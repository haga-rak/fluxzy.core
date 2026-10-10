// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2MaxFrameSizeSettingTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16383)]
        [InlineData(16777216)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void OutOfRangeValueIsProtocolError(int value)
        {
            var settingFrame = new SettingFrame(SettingIdentifier.SettingsMaxFrameSize, value);

            Assert.True(H2Helper.TryGetSettingError(ref settingFrame, out var errorCode));
            Assert.Equal(H2ErrorCode.ProtocolError, errorCode);
        }

        [Theory]
        [InlineData(16384)]
        [InlineData(65536)]
        [InlineData(1024 * 1024)]
        [InlineData(16777215)]
        public void InRangeValueIsAccepted(int value)
        {
            var settingFrame = new SettingFrame(SettingIdentifier.SettingsMaxFrameSize, value);

            Assert.False(H2Helper.TryGetSettingError(ref settingFrame, out _));
        }

        [Fact]
        public void OtherSettingsAreNotRangeChecked()
        {
            var settingFrame = new SettingFrame(SettingIdentifier.SettingsMaxConcurrentStreams, 0);

            Assert.False(H2Helper.TryGetSettingError(ref settingFrame, out _));
        }

        [Theory]
        [InlineData(16383)]
        [InlineData(16777216)]
        public async Task DownstreamRejectsOutOfRangeValueWithGoAway(int value)
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(H2FrameType.Settings);

            await ctx.SendSettingsFrame((SettingIdentifier.SettingsMaxFrameSize, value));

            var frame = await ctx.ReadUntilFrameType(H2FrameType.Goaway);
            frame.GetGoAwayFrame().Read(out var errorCode, out _);

            Assert.Equal(H2ErrorCode.ProtocolError, errorCode);
        }

        [Theory]
        [InlineData(16384)]
        [InlineData(16777215)]
        public async Task DownstreamAcknowledgesBoundaryValues(int value)
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(H2FrameType.Settings);

            await ctx.SendSettingsFrame((SettingIdentifier.SettingsMaxFrameSize, value));

            for (var i = 0; i < 10; i++) {
                var frame = await ctx.ReadNextFrame();

                Assert.NotEqual(H2FrameType.Goaway, frame.BodyType);

                if (frame.BodyType == H2FrameType.Settings && frame.Flags.HasFlag(HeaderFlags.Ack))
                    return;
            }

            Assert.Fail("Server did not acknowledge the SETTINGS frame");
        }
    }
}
