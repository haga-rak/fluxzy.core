// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Rules.Session;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Session
{
    public class SessionCookieTests
    {
        [Fact]
        public void Constructor_InitializesNameAndValue()
        {
            // Arrange & Act
            var cookie = new SessionCookie("test", "value");

            // Assert
            Assert.Equal("test", cookie.Name);
            Assert.Equal("value", cookie.Value);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var cookie = new SessionCookie("test", "value");

            // Assert
            Assert.Null(cookie.Path);
            Assert.Null(cookie.Domain);
            Assert.Null(cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenNoExpires()
        {
            // Arrange
            var cookie = new SessionCookie("test", "value");

            // Act & Assert
            Assert.False(cookie.IsExpired());
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenFutureExpires()
        {
            // Arrange
            var cookie = new SessionCookie("test", "value")
            {
                Expires = DateTime.UtcNow.AddHours(1)
            };

            // Act & Assert
            Assert.False(cookie.IsExpired());
        }

        [Fact]
        public void IsExpired_ReturnsTrue_WhenPastExpires()
        {
            // Arrange
            var cookie = new SessionCookie("test", "value")
            {
                Expires = DateTime.UtcNow.AddHours(-1)
            };

            // Act & Assert
            Assert.True(cookie.IsExpired());
        }

        [Fact]
        public void Value_CanBeUpdated()
        {
            // Arrange
            var cookie = new SessionCookie("test", "original");

            // Act
            cookie.Value = "updated";

            // Assert
            Assert.Equal("updated", cookie.Value);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/api")]
        [InlineData("/api/v1/users")]
        public void Path_CanBeSet(string path)
        {
            // Arrange
            var cookie = new SessionCookie("test", "value");

            // Act
            cookie.Path = path;

            // Assert
            Assert.Equal(path, cookie.Path);
        }

        [Theory]
        [InlineData("example.com")]
        [InlineData(".example.com")]
        [InlineData("sub.example.com")]
        public void Domain_CanBeSet(string domain)
        {
            // Arrange
            var cookie = new SessionCookie("test", "value");

            // Act
            cookie.Domain = domain;

            // Assert
            Assert.Equal(domain, cookie.Domain);
        }
    }
}
