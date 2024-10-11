// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Readers;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class IdLookupFilterTests
    {
        [Theory]
        [InlineData("95,97", "95,97")]
        [InlineData("95-97", "95,96,97")]
        public void CheckPass_SearchTextFilter(
            string pattern, string expectedResult)
        {
            var archiveFile = "_Files/Archives/pink-floyd.fxzy";
            var expectedResultList  = expectedResult.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(int.Parse)
                                                    .ToHashSet();

            var filter = new IdLookupFilter()
            {
                Pattern = pattern
            };

            var archiveReader = new FluxzyArchiveReader(archiveFile);
            var actualResultList = new HashSet<int>();
            var exchanges = archiveReader.ReadAllExchanges().ToList();

            filter.Init(null!);

            foreach (var exchange in exchanges) {

                var filteringContext = new ExchangeInfoFilteringContext(archiveReader,
                    exchange.Id);

                var filterResult = filter.Apply(null, null!, exchange, filteringContext);

                if (filterResult) {
                    actualResultList.Add(exchange.Id);
                }
            }

            Assert.Equal(expectedResultList, actualResultList);
        }
    }
}
