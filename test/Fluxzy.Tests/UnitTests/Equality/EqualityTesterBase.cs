using System.Collections.Generic;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public abstract class EqualityTesterBase<T>
    {
        protected abstract T Item { get; }

        protected abstract IEnumerable<T> EqualItems { get; }

        protected abstract IEnumerable<T> NotEqualItems { get; }

        [Fact]
        public void Test_Same_Ref()
        {
            Assert.Equal(Item, Item);
        }

        [Fact]
        public void Test_With_Null()
        {
            Assert.NotEqual(default, Item);
            Assert.NotEqual(Item, default);
        }

        [Fact]
        public void Test_Equal()
        {
            foreach (var equal in EqualItems)
            {
                var set = new HashSet<T> { equal };
                Assert.Contains(Item, set);
            }
        }

        [Fact]
        public void Test_Not_Equal()
        {
            foreach (var notEqual in NotEqualItems)
            {
                var set = new HashSet<T> { notEqual };
                Assert.DoesNotContain(Item, set);
            }
        }
    }
}
