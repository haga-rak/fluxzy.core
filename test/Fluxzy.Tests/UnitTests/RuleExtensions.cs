using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests
{
    public class RuleExtensions
    {
        [Fact]
        public void Test_When_Any()
        {
            var setting = FluxzySetting.CreateDefault();

            var action = new AddRequestHeaderAction("x", "y");

            setting.ClearAlterationRules();

            setting.ConfigureRule().WhenAny().Do(action);

            Assert.Single(setting.AlterationRules);
            Assert.Equal(action, setting.AlterationRules.First().Action);
            Assert.Equal(AnyFilter.Default, setting.AlterationRules.First().Filter);
        }

        [Fact]
        public void Test_When_Multiple_Actions()
        {
            var setting = FluxzySetting.CreateDefault();

            var filter = new HostFilter("myhost.com", StringSelectorOperation.Contains); 
            var actionA = new AddRequestHeaderAction("x", "y");
            var actionB = new ForceHttp11Action();

            setting.ClearAlterationRules();

            setting.ConfigureRule().When(filter).Do(actionA, actionB);

            Assert.Equal(2, setting.AlterationRules.Count);
            Assert.Equal(actionA, setting.AlterationRules.First().Action);
            Assert.Equal(actionB, setting.AlterationRules.Last().Action);
        }

        [Fact]
        public void Test_When_Any_Multiple_Actions()
        {
            var setting = FluxzySetting.CreateDefault();

            var filterA = new HostFilter("myhost.com", StringSelectorOperation.Contains);
            var filterB = new StatusCodeSuccessFilter();

            var actionA = new AddRequestHeaderAction("x", "y");
            var actionB = new ForceHttp11Action();

            setting.ClearAlterationRules();

            setting.ConfigureRule().WhenAny(filterA, filterB).Do(actionA, actionB);

            Assert.Equal(2, setting.AlterationRules.Count);
            Assert.Equal(actionA, setting.AlterationRules.First().Action);
            Assert.Equal(actionB, setting.AlterationRules.Last().Action);
            Assert.Equal(typeof(FilterCollection), setting.AlterationRules.Last().Filter.GetType());
            Assert.Equal(SelectorCollectionOperation.Or, ((FilterCollection) setting.AlterationRules.Last().Filter).Operation);
        }

        [Fact]
        public void Test_When_All()
        {
            var setting = FluxzySetting.CreateDefault();

            var filterA = new HostFilter("myhost.com", StringSelectorOperation.Contains);
            var filterB = new StatusCodeSuccessFilter();

            var actionA = new AddRequestHeaderAction("x", "y");
            var actionB = new ForceHttp11Action();

            setting.ClearAlterationRules();

            setting.ConfigureRule().WhenAll(filterA, filterB).Do(actionA, actionB);

            Assert.Equal(2, setting.AlterationRules.Count);
            Assert.Equal(actionA, setting.AlterationRules.First().Action);
            Assert.Equal(actionB, setting.AlterationRules.Last().Action);
            Assert.Equal(typeof(FilterCollection), setting.AlterationRules.Last().Filter.GetType());
            Assert.Equal(SelectorCollectionOperation.And, ((FilterCollection)setting.AlterationRules.Last().Filter).Operation);
        }
    }
}
