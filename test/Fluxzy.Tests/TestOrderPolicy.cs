// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

[assembly: TestCollectionOrderer("Fluxzy.Tests.TestOrderPolicy", "Fluxzy.Tests")]

namespace Fluxzy.Tests;

public class TestOrderPolicy : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        var allCollections = testCollections.ToList();

        return allCollections
            .OrderBy(collection => collection.DisplayName.Contains("[Run last]", StringComparison.OrdinalIgnoreCase));
    }
}