// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Rules.Filters.ViewOnlyFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class DefaultFilterSettings
    {
        private static readonly Dictionary<Type, Func<Filter>>
            FilterFactory = new() {
                [typeof(AuthorityFilter)] = () => new AuthorityFilter(
                    443, "google.com", StringSelectorOperation.EndsWith),
                [typeof(ConnectionFilter)] = () => new ConnectionFilter(1),
                [typeof(TagContainsFilter)] = () => new TagContainsFilter(new Tag("s")),
            };

        static DefaultFilterSettings()
        {
            void AddFilter(Filter t)
            {
                FilterFactory.Add(t.GetType(), () => t);
            }

            AddFilter(new ExecFilter("true", ""));
            AddFilter(new PathFilter("/yes"));
            AddFilter(new AbsoluteUriFilter("/yes"));
            AddFilter(new QueryStringFilter("name", "value"));
            AddFilter(new AgentFilter(new (1, "a")));
            AddFilter(new MethodFilter("POST"));
            AddFilter(new SearchTextFilter("pattern"));
            AddFilter(new HostFilter("google.com"));
            AddFilter(new RequestHeaderFilter("name", "value"));
            AddFilter(new ResponseHeaderFilter("name", "value"));
            AddFilter(new HasSetCookieOnResponseFilter("name"));
            AddFilter(new HasCookieOnRequestFilter("name", "value"));
        }


        public static IEnumerable<object[]> GetFilterTypes()
        {
            return typeof(Filter).Assembly
                          .GetTypes()
                          .Where(t => t.IsSubclassOf(typeof(Filter)))
                          .Where(t => !t.IsAbstract)
                          // having a default constructor is a requirement
                          //.Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                          .Select(s => new object [] { s.FullName! })
                          .ToList();
        }

        [Theory]
        [MemberData(nameof(GetFilterTypes))]
        public void ValidateProperties(string filterName)
        {
            var filter = CreateInstance(filterName);

            if (filter == null)
                return; 

            Assert.NotNull(filter.Common);
            Assert.NotNull(filter.GenericName);
            Assert.NotNull(filter.PreMadeFilter);
            Assert.NotNull(filter.FilterScope);

            Assert.NotNull(filter.AutoGeneratedName);
            Assert.NotNull(filter.Category);
            Assert.NotNull(filter.ShortName);
            Assert.NotNull(filter.GenericName);
        }

        private static Filter? CreateInstance(string filterName)
        {
            var type = typeof(Filter).Assembly.GetType(filterName)!;

            if (type.GetConstructor(Type.EmptyTypes) != null) {
                var filter = (Filter)Activator.CreateInstance(type!)!;
                return filter;
            }

            if (FilterFactory.TryGetValue(type, out var func)) {
                return func(); 
            }

            return null; 
        }

        [Theory]
        [MemberData(nameof(GetFilterTypes))]
        public void ValidateExamples(string filterName)
        {
            var filter = CreateInstance(filterName);

            if (filter == null)
                return;

            var examples = filter.GetExamples()?.ToList();

            Assert.NotNull(examples);

            foreach (var example in examples!) {
                Assert.NotNull(example.Description);
                Assert.NotNull(example.Filter);
            }
        }
    }
}
