// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Rules.Session;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Session
{
    public class SessionDataTests
    {
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var session = new SessionData("example.com");

            // Assert
            Assert.Equal("example.com", session.Domain);
            Assert.NotNull(session.Cookies);
            Assert.NotNull(session.Headers);
            Assert.Empty(session.Cookies);
            Assert.Empty(session.Headers);
        }

        [Fact]
        public void SetCookie_AddsCookie()
        {
            // Arrange
            var session = new SessionData("example.com");

            // Act
            session.SetCookie("sessionId", "abc123");

            // Assert
            Assert.True(session.Cookies.ContainsKey("sessionId"));
            Assert.Equal("abc123", session.Cookies["sessionId"].Value);
        }

        [Fact]
        public void SetCookie_WithPath_StoresPath()
        {
            // Arrange
            var session = new SessionData("example.com");

            // Act
            session.SetCookie("sessionId", "abc123", "/api");

            // Assert
            Assert.Equal("/api", session.Cookies["sessionId"].Path);
        }

        [Fact]
        public void SetCookie_WithExpires_StoresExpires()
        {
            // Arrange
            var session = new SessionData("example.com");
            var expires = DateTime.UtcNow.AddHours(1);

            // Act
            session.SetCookie("sessionId", "abc123", null, expires);

            // Assert
            Assert.Equal(expires, session.Cookies["sessionId"].Expires);
        }

        [Fact]
        public void SetCookie_UpdatesExistingCookie()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("sessionId", "abc123");

            // Act
            session.SetCookie("sessionId", "xyz789");

            // Assert
            Assert.Single(session.Cookies);
            Assert.Equal("xyz789", session.Cookies["sessionId"].Value);
        }

        [Fact]
        public void SetHeader_AddsHeader()
        {
            // Arrange
            var session = new SessionData("example.com");

            // Act
            session.SetHeader("Authorization", "Bearer token123");

            // Assert
            Assert.True(session.Headers.ContainsKey("Authorization"));
            Assert.Equal("Bearer token123", session.Headers["Authorization"]);
        }

        [Fact]
        public void SetHeader_UpdatesExistingHeader()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetHeader("Authorization", "Bearer old");

            // Act
            session.SetHeader("Authorization", "Bearer new");

            // Assert
            Assert.Single(session.Headers);
            Assert.Equal("Bearer new", session.Headers["Authorization"]);
        }

        [Fact]
        public void GetCookieHeaderValue_ReturnsFormattedString()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("cookie1", "value1");
            session.SetCookie("cookie2", "value2");

            // Act
            var result = session.GetCookieHeaderValue();

            // Assert
            Assert.Contains("cookie1=value1", result);
            Assert.Contains("cookie2=value2", result);
            Assert.Contains("; ", result);
        }

        [Fact]
        public void GetCookieHeaderValue_UrlEncodesCookies()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("name with space", "value=with;special");

            // Act
            var result = session.GetCookieHeaderValue();

            // Assert
            Assert.Contains("name+with+space", result);
            Assert.Contains("value%3dwith%3bspecial", result);
        }

        [Fact]
        public void GetCookieHeaderValue_ExcludesExpiredCookies()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("valid", "value1", null, DateTime.UtcNow.AddHours(1));
            session.SetCookie("expired", "value2", null, DateTime.UtcNow.AddHours(-1));

            // Act
            var result = session.GetCookieHeaderValue();

            // Assert
            Assert.Contains("valid", result);
            Assert.DoesNotContain("expired", result);
        }

        [Fact]
        public void RemoveExpiredCookies_RemovesExpiredOnly()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("valid", "value1", null, DateTime.UtcNow.AddHours(1));
            session.SetCookie("expired", "value2", null, DateTime.UtcNow.AddHours(-1));

            // Act
            session.RemoveExpiredCookies();

            // Assert
            Assert.True(session.Cookies.ContainsKey("valid"));
            Assert.False(session.Cookies.ContainsKey("expired"));
        }

        [Fact]
        public void CookieLookup_IsCaseInsensitive()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetCookie("SessionId", "value");

            // Act & Assert
            Assert.True(session.Cookies.ContainsKey("sessionid"));
            Assert.True(session.Cookies.ContainsKey("SESSIONID"));
        }

        [Fact]
        public void HeaderLookup_IsCaseInsensitive()
        {
            // Arrange
            var session = new SessionData("example.com");
            session.SetHeader("Authorization", "Bearer token");

            // Act & Assert
            Assert.True(session.Headers.ContainsKey("authorization"));
            Assert.True(session.Headers.ContainsKey("AUTHORIZATION"));
        }

        [Fact]
        public void LastUpdated_IsUpdatedOnSetCookie()
        {
            // Arrange
            var session = new SessionData("example.com");
            var initialTime = session.LastUpdated;

            // Small delay to ensure time difference
            System.Threading.Thread.Sleep(10);

            // Act
            session.SetCookie("test", "value");

            // Assert
            Assert.True(session.LastUpdated > initialTime);
        }

        [Fact]
        public void LastUpdated_IsUpdatedOnSetHeader()
        {
            // Arrange
            var session = new SessionData("example.com");
            var initialTime = session.LastUpdated;

            // Small delay to ensure time difference
            System.Threading.Thread.Sleep(10);

            // Act
            session.SetHeader("X-Test", "value");

            // Assert
            Assert.True(session.LastUpdated > initialTime);
        }
    }
}
