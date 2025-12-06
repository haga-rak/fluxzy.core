// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class HotReloadTests
    {
        [Fact]
        public async Task UpdateRules_BeforeProxyStart_ThrowsInvalidOperationException()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);

            var newRules = new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Test", "value"), AnyFilter.Default)
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => proxy.UpdateRules(newRules));
            Assert.Contains("not been started", exception.Message);
        }

        [Fact]
        public async Task UpdateRules_AfterDispose_ThrowsInvalidOperationException()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            await proxy.DisposeAsync();

            var newRules = new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Test", "value"), AnyFilter.Default)
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => proxy.UpdateRules(newRules));
            Assert.Contains("disposed", exception.Message);
        }

        [Fact]
        public async Task UpdateRules_WithNullRules_ThrowsArgumentNullException()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => proxy.UpdateRules((IEnumerable<Rule>)null!));
        }

        [Fact]
        public async Task UpdateRules_WithNullConfigureAction_ThrowsArgumentNullException()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => proxy.UpdateRules((Action<FluxzySetting>)null!));
        }

        [Fact]
        public async Task UpdateRules_WithValidRules_UpdatesSuccessfully()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Initial", "initial"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            var newRules = new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Updated", "updated"), AnyFilter.Default)
            };

            // Act
            proxy.UpdateRules(newRules);

            // Assert
            var activeRules = proxy.GetActiveRules();
            Assert.Single(activeRules);
            Assert.IsType<AddResponseHeaderAction>(activeRules.First().Action);
            var action = (AddResponseHeaderAction)activeRules.First().Action;
            Assert.Equal("X-Updated", action.HeaderName);
            Assert.Equal("updated", action.HeaderValue);
        }

        [Fact]
        public async Task UpdateRules_WithConfigureAction_UpdatesSuccessfully()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act
            proxy.UpdateRules(s => {
                s.AddAlterationRules(
                    new AddResponseHeaderAction("X-Configured", "configured"),
                    AnyFilter.Default
                );
            });

            // Assert
            var activeRules = proxy.GetActiveRules();
            Assert.Single(activeRules);
            Assert.IsType<AddResponseHeaderAction>(activeRules.First().Action);
            var action = (AddResponseHeaderAction)activeRules.First().Action;
            Assert.Equal("X-Configured", action.HeaderName);
        }

        [Fact]
        public async Task UpdateRules_WithEmptyList_SucceedsWithOnlyFixedRules()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Initial", "initial"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act
            proxy.UpdateRules(new List<Rule>());

            // Assert - only fixed rules remain, alteration rules are cleared
            var activeRules = proxy.GetActiveRules();
            Assert.Empty(activeRules); // GetActiveRules excludes fixed rules
        }

        [Fact]
        public async Task GetActiveRules_ReturnsCurrentRulesWithoutFixed()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Test1", "value1"),
                AnyFilter.Default
            );
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Test2", "value2"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act
            var activeRules = proxy.GetActiveRules();

            // Assert
            Assert.Equal(2, activeRules.Count);
            // Fixed rules should not be included
            Assert.All(activeRules, rule => {
                Assert.False(rule.Action is SkipSslTunnelingAction);
                Assert.False(rule.Action is MountCertificateAuthorityAction);
                Assert.False(rule.Action is MountWelcomePageAction);
            });
        }

        [Fact]
        public async Task UpdateRules_MultipleConsecutiveCalls_UpdatesSuccessfully()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act - Update multiple times
            proxy.UpdateRules(new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-First", "first"), AnyFilter.Default)
            });

            proxy.UpdateRules(new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Second", "second"), AnyFilter.Default)
            });

            proxy.UpdateRules(new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Third", "third"), AnyFilter.Default)
            });

            // Assert - Should have the last update
            var activeRules = proxy.GetActiveRules();
            Assert.Single(activeRules);
            var action = (AddResponseHeaderAction)activeRules.First().Action;
            Assert.Equal("X-Third", action.HeaderName);
        }

        [Fact]
        public async Task UpdateRules_WithMultipleRules_PreservesAllRules()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            // Act
            var newRules = new List<Rule>
            {
                new Rule(new AddResponseHeaderAction("X-Header1", "value1"), AnyFilter.Default),
                new Rule(new AddResponseHeaderAction("X-Header2", "value2"), AnyFilter.Default),
                new Rule(new AddResponseHeaderAction("X-Header3", "value3"), AnyFilter.Default)
            };

            proxy.UpdateRules(newRules);

            // Assert
            var activeRules = proxy.GetActiveRules();
            Assert.Equal(3, activeRules.Count);
        }

        [Fact]
        public async Task GetActiveRules_BeforeStart_ThrowsInvalidOperationException()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            var proxy = new Proxy(setting);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => proxy.GetActiveRules());
            Assert.Contains("not been started", exception.Message);
        }

        [Fact]
        public async Task UpdateRules_PreservesRuleFilterAndAction()
        {
            // Arrange
            var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            await using var proxy = new Proxy(setting);
            _ = proxy.Run();

            var customFilter = new MethodFilter("POST");
            var customAction = new AddRequestHeaderAction("X-Custom", "custom-value");

            // Act
            proxy.UpdateRules(new List<Rule>
            {
                new Rule(customAction, customFilter)
            });

            // Assert
            var activeRules = proxy.GetActiveRules();
            var rule = activeRules.First();
            Assert.IsType<MethodFilter>(rule.Filter);
            Assert.IsType<AddRequestHeaderAction>(rule.Action);
        }
    }
}
