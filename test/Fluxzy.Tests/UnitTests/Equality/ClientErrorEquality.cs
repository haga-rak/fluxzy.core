// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class ClientErrorEquality : EqualityTesterBase<ClientError>
    {
        protected override ClientError Item { get; } = new ClientError(9, "message");

        protected override IEnumerable<ClientError> EqualItems { get; } =
            new[] { new ClientError(9, "message") };

        protected override IEnumerable<ClientError> NotEqualItems { get; }
            = new[] {
                new ClientError(9, "messages"),
                new ClientError(90, "message"),
            };
    }
}
