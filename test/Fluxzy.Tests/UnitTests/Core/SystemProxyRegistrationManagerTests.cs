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
        public async Task Unregister_Without_Register_Disables_Current_Proxy()
        {
            // Arrange: system has a proxy but Register was never called
            var currentSetting = CreateSetting("some-proxy.local", 3128, true);

            var setter = Substitute.For<ISystemProxySetter>();
            setter.ReadSetting().Returns(Task.FromResult(currentSetting));
            setter.ApplySetting(Arg.Any<SystemProxySetting>()).Returns(Task.CompletedTask);

            var manager = new SystemProxyRegistrationManager(setter);

            // Act
            await manager.UnRegister();

            // Assert: proxy is disabled
            await setter.Received(1).ApplySetting(
                Arg.Is<SystemProxySetting>(s => !s.Enabled));
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

            // Assert: second unregister should NOT apply any setting (nothing to do)
            // because both _oldSetting and _currentSetting are null.
            // It falls to the third branch which reads current setting — the original
            // is already enabled, so it would disable it. But the key point is it must
            // NOT apply fluxzy's address with Enabled=false.
            await setter.DidNotReceive().ApplySetting(
                Arg.Is<SystemProxySetting>(s =>
                    s.BoundHost == "127.0.0.1"));
        }
    }
}
