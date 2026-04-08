// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Proxy;
using NSubstitute;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class SystemProxyRegistrationManagerTests
    {
        private static SystemProxySetting CreateSetting(
            string host, int port, bool enabled, params string[] bypass)
        {
            return new SystemProxySetting(host, port, bypass) { Enabled = enabled };
        }

        [Fact]
        public async Task Register_Should_Save_Original_Setting()
        {
            // Arrange: system already has a proxy configured
            var originalSetting = CreateSetting("corp-proxy.local", 3128, true, "*.local");

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 8080);

            // Act
            await manager.Register(endpoint, "localhost");

            // Assert: the setter was called with fluxzy's proxy, not the original
            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.BoundHost == "127.0.0.1" && s.ListenPort == 8080));
        }

        [Fact]
        public async Task Unregister_Should_Restore_Original_Setting()
        {
            // Arrange: system already has a proxy configured
            var originalSetting = CreateSetting("corp-proxy.local", 3128, true, "*.local");

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 8080);

            await manager.Register(endpoint, "localhost");

            // Act
            await manager.UnRegister();

            // Assert: the original setting was restored (last call to ApplySetting)
            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.BoundHost == "corp-proxy.local"
                    && s.ListenPort == 3128
                    && s.Enabled));
        }

        [Fact]
        public async Task Unregister_Should_Restore_Disabled_State_When_No_Proxy_Was_Set()
        {
            // Arrange: system had no proxy
            var originalSetting = CreateSetting("no_proxy_server", -1, false);

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 8080);

            await manager.Register(endpoint, "localhost");

            // Act
            await manager.UnRegister();

            // Assert: the disabled original is restored, not fluxzy's address
            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.BoundHost == "no_proxy_server"
                    && !s.Enabled));
        }

        [Fact]
        public async Task Register_Multiple_Times_Should_Keep_First_Original_Setting()
        {
            // Arrange
            var originalSetting = CreateSetting("corp-proxy.local", 3128, true, "*.local");

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);

            // Act: register, then re-register on a different port
            await manager.Register(new IPEndPoint(IPAddress.Loopback, 8080), "localhost");

            // After first register, ReadSetting now returns fluxzy's own setting
            var fluxzySetting = CreateSetting("127.0.0.1", 8080, true, "localhost");
            setter.ReadSetting().Returns(Task.FromResult(fluxzySetting));

            await manager.Register(new IPEndPoint(IPAddress.Loopback, 9090), "localhost");

            // Unregister should still restore the *original*, not fluxzy's first setting
            await manager.UnRegister();

            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.BoundHost == "corp-proxy.local"
                    && s.ListenPort == 3128));
        }

        [Fact]
        public async Task Unregister_Without_Register_Is_A_NoOp()
        {
            // Arrange: system has a proxy but Register was never called
            var currentSetting = CreateSetting("some-proxy.local", 3128, true);

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(currentSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);

            // Act
            await manager.UnRegister();

            // Assert: we must NOT touch the user's proxy when we never registered.
            // The previous fallback path disabled the current proxy, which corrupted
            // state when the ProcessExit handler ran after an explicit UnRegister.
            await setter.DidNotReceive().ApplySetting(Arg.Any<SystemProxySetting>());
        }

        [Fact]
        public async Task Double_Unregister_Should_Not_Overwrite_Restored_Setting()
        {
            // Arrange: simulates Register → UnRegister → ProcessExit handler calls UnRegister again
            var originalSetting = CreateSetting("corp-proxy.local", 3128, true, "*.local");

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);

            await manager.Register(new IPEndPoint(IPAddress.Loopback, 8080), "localhost");

            // Act: unregister twice (first explicit, second from ProcessExit handler)
            await manager.UnRegister();
            setter.ClearReceivedCalls();

            // After first unregister, ReadSetting returns the restored original
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));

            await manager.UnRegister();

            // Assert: second unregister must be a complete no-op. Both _oldSetting
            // and _currentSetting are null after the first restore, so we must not
            // touch the user's proxy again. The previous fallback branch would
            // disable the just-restored original, which is wrong.
            await setter.DidNotReceive().ApplySetting(Arg.Any<SystemProxySetting>());
        }

        [Fact]
        public async Task Unregister_Should_Preserve_Raw_ProxyServer_For_Legacy_Formats()
        {
            // Arrange: Windows registry contains a legacy per-protocol ProxyServer
            // (e.g. "http=proxy:80;https=proxy:443"). The Windows parser collapses
            // the BoundHost to "no_proxy_server" because it is not a plain host:port.
            // Without preserving the raw string, UnRegister would delete ProxyServer
            // entirely, destroying the user's corporate configuration.
            const string rawLegacyProxyServer = "http=corp-proxy.local:3128;https=corp-proxy.local:3128";

            var originalSetting = new SystemProxySetting("no_proxy_server", -1) { Enabled = true };
            originalSetting.PrivateValues["WinProxyServerRaw"] = rawLegacyProxyServer;

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(originalSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);
            await manager.Register(new IPEndPoint(IPAddress.Loopback, 8080), "localhost");

            // Act
            await manager.UnRegister();

            // Assert: on restore the setting carries the raw legacy string so the
            // Windows writer can round-trip it verbatim.
            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.Enabled
                    && s.PrivateValues.ContainsKey("WinProxyServerRaw")
                    && (string) s.PrivateValues["WinProxyServerRaw"] == rawLegacyProxyServer));
        }
    }
}
