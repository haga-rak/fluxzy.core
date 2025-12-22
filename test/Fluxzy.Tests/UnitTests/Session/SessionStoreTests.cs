// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Rules.Session;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Session
{
    public class SessionStoreTests
    {
        [Fact]
        public void GetSession_NonExistent_ReturnsNull()
        {
            // Arrange
            var store = new SessionStore();

            // Act
            var result = store.GetSession("nonexistent.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetOrCreateSession_CreatesNewSession()
        {
            // Arrange
            var store = new SessionStore();

            // Act
            var session = store.GetOrCreateSession("example.com");

            // Assert
            Assert.NotNull(session);
            Assert.Equal("example.com", session.Domain);
        }

        [Fact]
        public void GetOrCreateSession_ReturnsSameSession()
        {
            // Arrange
            var store = new SessionStore();

            // Act
            var session1 = store.GetOrCreateSession("example.com");
            var session2 = store.GetOrCreateSession("example.com");

            // Assert
            Assert.Same(session1, session2);
        }

        [Fact]
        public void SetSession_StoresSession()
        {
            // Arrange
            var store = new SessionStore();
            var sessionData = new SessionData("example.com");
            sessionData.SetCookie("test", "value");

            // Act
            store.SetSession("example.com", sessionData);
            var retrieved = store.GetSession("example.com");

            // Assert
            Assert.NotNull(retrieved);
            Assert.True(retrieved.Cookies.ContainsKey("test"));
        }

        [Fact]
        public void ClearSession_RemovesSession()
        {
            // Arrange
            var store = new SessionStore();
            store.GetOrCreateSession("example.com");

            // Act
            store.ClearSession("example.com");
            var result = store.GetSession("example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ClearAll_RemovesAllSessions()
        {
            // Arrange
            var store = new SessionStore();
            store.GetOrCreateSession("example1.com");
            store.GetOrCreateSession("example2.com");
            store.GetOrCreateSession("example3.com");

            // Act
            store.ClearAll();

            // Assert
            Assert.Equal(0, store.Count);
            Assert.Null(store.GetSession("example1.com"));
            Assert.Null(store.GetSession("example2.com"));
            Assert.Null(store.GetSession("example3.com"));
        }

        [Fact]
        public void Count_ReturnsCorrectNumber()
        {
            // Arrange
            var store = new SessionStore();

            // Act & Assert
            Assert.Equal(0, store.Count);

            store.GetOrCreateSession("example1.com");
            Assert.Equal(1, store.Count);

            store.GetOrCreateSession("example2.com");
            Assert.Equal(2, store.Count);

            store.ClearSession("example1.com");
            Assert.Equal(1, store.Count);
        }

        [Fact]
        public void DomainLookup_IsCaseInsensitive()
        {
            // Arrange
            var store = new SessionStore();
            var session = store.GetOrCreateSession("Example.COM");

            // Act
            var retrieved = store.GetSession("example.com");

            // Assert
            Assert.Same(session, retrieved);
        }

        [Fact]
        public async Task ConcurrentAccess_IsThreadSafe()
        {
            // Arrange
            var store = new SessionStore();
            var tasks = new Task[100];

            // Act
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    var domain = $"domain{index % 10}.com";
                    var session = store.GetOrCreateSession(domain);
                    session.SetCookie($"cookie{index}", $"value{index}");
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(10, store.Count);
        }
    }
}
