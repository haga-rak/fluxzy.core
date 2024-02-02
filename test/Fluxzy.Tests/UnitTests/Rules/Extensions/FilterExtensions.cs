// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Extensions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules.Extensions
{
    public class FilterExtensions
    {
        private readonly FluxzySetting _setting;

        public FilterExtensions()
        {
            _setting = FluxzySetting.CreateDefault();
            _setting.ClearAlterationRules();
        }

        [Fact]
        public void ValidateWhenHost()
        {
            _setting.ConfigureRule()
                    .WhenHostMatch("fakehost.com")
                    .Do(new ApplyCommentAction("comment"));

            var targetFilter = _setting.AlterationRules.First().Filter as HostFilter; 

            Assert.Single(_setting.AlterationRules);
            Assert.NotNull(targetFilter);
            Assert.Equal("fakehost.com", targetFilter.Pattern);
            Assert.Equal(StringSelectorOperation.Regex, targetFilter.Operation);
        }

        [Fact]
        public void ValidateWhenHostIn()
        {
            _setting.ConfigureRule()
                    .WhenHostIn("fakehost.com")
                    .Do(new ApplyCommentAction("comment"));

            var targetFilter = _setting.AlterationRules.First().Filter as HostFilter; 

            Assert.Single(_setting.AlterationRules);
            Assert.NotNull(targetFilter);
            Assert.Equal("fakehost.com", targetFilter.Pattern);
            Assert.Equal(StringSelectorOperation.Exact, targetFilter.Operation);
        }

        [Fact]
        public void ValidateWhenHostInMultiple()
        {
            _setting.ConfigureRule()
                    .WhenHostIn("fakehost.com", "secondhost")
                    .Do(new ApplyCommentAction("comment"));

            ValidateWhenHostMultiple(StringSelectorOperation.Exact);
        }

        [Fact]
        public void ValidateWhenHostEndsWithMultiple()
        {
            _setting.ConfigureRule()
                    .WhenHostEndsWith("fakehost.com", "secondhost")
                    .Do(new ApplyCommentAction("comment"));

            ValidateWhenHostMultiple(StringSelectorOperation.EndsWith);
        }

        [Fact]
        public void ValidateHostContainMultiple()
        {
            _setting.ConfigureRule()
                    .WhenHostContain("fakehost.com", "secondhost")
                    .Do(new ApplyCommentAction("comment"));

            ValidateWhenHostMultiple(StringSelectorOperation.Contains);
        }

        private void ValidateWhenHostMultiple(StringSelectorOperation operation)
        {
            var targetFilter = _setting.AlterationRules.First().Filter as FilterCollection;
            var children = targetFilter?.Children.OfType<HostFilter>().ToList();

            Assert.Single(_setting.AlterationRules);
            Assert.NotNull(targetFilter);
            Assert.NotNull(children);
            Assert.Equal(2, children.Count);

            foreach (var filter in children) {
                Assert.Equal(operation, filter.Operation);
            }
        }

        [Theory]
        [MemberData(nameof(GenerateNoParamFilterExtension))]
        public void Validate(string expectedType, 
            Func<IConfigureFilterBuilder, IConfigureActionBuilder> extensionMethod)
        {
            extensionMethod(_setting.ConfigureRule()).Do(new ApplyCommentAction("comment"));

            var actualType = _setting.AlterationRules.First().Filter.GetType()?.FullName;

            Assert.Single(_setting.AlterationRules);
            Assert.Equal(expectedType, actualType);
        }

        public static IEnumerable<object[]> GenerateNoParamFilterExtension()
        {
            yield return 
                new object[] { typeof(MethodFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenMethodIsGet()) };

            yield return 
                new object[] { typeof(MethodFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenMethodIsPost()) };

            yield return 
                new object[] { typeof(MethodFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenMethodIsPut()) };

            yield return 
                new object[] { typeof(AuthorityFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenAuthorityMatch("host", 443)) };

            yield return 
                new object[] { typeof(PathFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenPathMatch("/path")) };

            yield return 
                new object[] { typeof(AbsoluteUriFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenUriMatch("/path")) };

            yield return 
                new object[] { typeof(JsonRequestFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenRequestHasJsonBody()) };

            yield return 
                new object[] { typeof(RequestHeaderFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenRequestHeaderExists("myheader")) };

            yield return 
                new object[] { typeof(RequestHeaderFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenRequestHeaderMatch("myheader", "myheadervalue")) };

            yield return 
                new object[] { typeof(StatusCodeServerErrorFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenServerError()) };

            yield return 
                new object[] { typeof(StatusCodeClientErrorFilter).FullName!, 
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenClientError()) };

            yield return
                new object[] { typeof(StatusCodeRedirectionFilter).FullName!,
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenRedirect()) };

            yield return
                new object[] { typeof(HtmlResponseFilter).FullName!,
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenResponseHasHtmlBody()) };

            yield return
                new object[] { typeof(JsonResponseFilter).FullName!,
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenResponseHasJsonBody()) };

            yield return
                new object[] { typeof(ResponseHeaderFilter).FullName!,
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenResponseHeaderExists("responseheader")) };

            yield return
                new object[] { typeof(ResponseHeaderFilter).FullName!,
                    new Func<IConfigureFilterBuilder, IConfigureActionBuilder>(builder => builder.WhenResponseHeaderMatch("responseheader", "value")) };

        }

    }
}
