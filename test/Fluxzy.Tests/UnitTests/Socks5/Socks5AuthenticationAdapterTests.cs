// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Core;
using Fluxzy.Core.Socks5;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Socks5
{
    public class Socks5AuthenticationAdapterTests
    {
        private static readonly IPEndPoint LocalEndPoint = new(IPAddress.Loopback, 1080);
        private static readonly IPEndPoint RemoteEndPoint = new(IPAddress.Loopback, 12345);

        [Fact]
        public void GetSocks5AuthMethod_NoAuth_ReturnsZero()
        {
            // Arrange
            var adapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);

            // Act
            var method = adapter.GetSocks5AuthMethod();

            // Assert
            Assert.Equal(Socks5Constants.AuthNoAuth, method);
        }

        [Fact]
        public void GetSocks5AuthMethod_Basic_ReturnsUsernamePassword()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("user", "pass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);

            // Act
            var method = adapter.GetSocks5AuthMethod();

            // Assert
            Assert.Equal(Socks5Constants.AuthUsernamePassword, method);
        }

        [Fact]
        public void RequiresAuthentication_NoAuth_ReturnsFalse()
        {
            // Arrange
            var adapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);

            // Assert
            Assert.False(adapter.RequiresAuthentication);
        }

        [Fact]
        public void RequiresAuthentication_Basic_ReturnsTrue()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("user", "pass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);

            // Assert
            Assert.True(adapter.RequiresAuthentication);
        }

        [Fact]
        public void ValidateCredentials_NoAuth_AlwaysReturnsTrue()
        {
            // Arrange
            var adapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);

            // Act
            var result = adapter.ValidateCredentials(LocalEndPoint, RemoteEndPoint, "any", "credentials");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCredentials_ValidBasic_ReturnsTrue()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("testuser", "testpass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);

            // Act
            var result = adapter.ValidateCredentials(LocalEndPoint, RemoteEndPoint, "testuser", "testpass");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCredentials_InvalidBasic_ReturnsFalse()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("testuser", "testpass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);

            // Act
            var result = adapter.ValidateCredentials(LocalEndPoint, RemoteEndPoint, "wronguser", "wrongpass");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateCredentials_WrongPassword_ReturnsFalse()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("testuser", "testpass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);

            // Act
            var result = adapter.ValidateCredentials(LocalEndPoint, RemoteEndPoint, "testuser", "wrongpass");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SelectAuthMethod_NoAuth_ClientSupportsNoAuth_ReturnsNoAuth()
        {
            // Arrange
            var adapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);
            var clientMethods = new byte[] { Socks5Constants.AuthNoAuth };

            // Act
            var selected = adapter.SelectAuthMethod(clientMethods);

            // Assert
            Assert.Equal(Socks5Constants.AuthNoAuth, selected);
        }

        [Fact]
        public void SelectAuthMethod_NoAuth_ClientDoesNotSupportNoAuth_ReturnsNoAcceptable()
        {
            // Arrange
            var adapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);
            var clientMethods = new byte[] { Socks5Constants.AuthUsernamePassword };

            // Act
            var selected = adapter.SelectAuthMethod(clientMethods);

            // Assert
            Assert.Equal(Socks5Constants.AuthNoAcceptable, selected);
        }

        [Fact]
        public void SelectAuthMethod_Basic_ClientSupportsUsernamePassword_ReturnsUsernamePassword()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("user", "pass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);
            var clientMethods = new byte[] { Socks5Constants.AuthNoAuth, Socks5Constants.AuthUsernamePassword };

            // Act
            var selected = adapter.SelectAuthMethod(clientMethods);

            // Assert
            Assert.Equal(Socks5Constants.AuthUsernamePassword, selected);
        }

        [Fact]
        public void SelectAuthMethod_Basic_ClientOnlySupportsNoAuth_ReturnsNoAcceptable()
        {
            // Arrange
            var authMethod = new BasicAuthenticationMethod("user", "pass");
            var adapter = new Socks5AuthenticationAdapter(authMethod);
            var clientMethods = new byte[] { Socks5Constants.AuthNoAuth };

            // Act
            var selected = adapter.SelectAuthMethod(clientMethods);

            // Assert
            Assert.Equal(Socks5Constants.AuthNoAcceptable, selected);
        }
    }
}
