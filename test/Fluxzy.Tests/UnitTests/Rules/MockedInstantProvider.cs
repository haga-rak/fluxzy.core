// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Actions;

namespace Fluxzy.Tests.UnitTests.Rules
{
    internal class MockedInstantProvider : IInstantProvider
    {
        private long _counter;

        public long ElapsedMillis {
            get
            {
                return (_counter++ * 1000);
            }
        }
    }
}
