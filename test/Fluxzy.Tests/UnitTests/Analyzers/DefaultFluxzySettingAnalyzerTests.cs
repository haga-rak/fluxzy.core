using System.Linq;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Validators;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Analyzers
{
    public class DefaultFluxzySettingAnalyzerTests
    {
        [Fact]
        public void Test_Empty()
        {
            var fluxzySetting = FluxzySetting.CreateDefault(); 
            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting).ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void Test_One()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.AddAlterationRulesForAny(new DelayAction(100));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting).ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Information, results[0].Level);
        }

        [Fact]
        public void Test_SkipSslEnableValidator()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.SetSkipGlobalSslDecryption(true);
            fluxzySetting.AddAlterationRulesForAny(new DelayAction(100));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting)
                                  .Where(r => r.Level == ValidationRuleLevel.Warning)
                                  .ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Warning, results[0].Level);
            Assert.Equal(nameof(SkipSslEnableValidator), results[0].SenderName);
        }

        [Fact]
        public void Test_OutOfScopeValidator()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.AddAlterationRules(new Rule(new AddRequestHeaderAction("A", "B"), new StatusCodeSuccessFilter()));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting)
                                  .Where(r => r.Level == ValidationRuleLevel.Warning)
                                  .ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Warning, results[0].Level);
            Assert.Equal(nameof(OutOfScopeValidator), results[0].SenderName);
        }

        [Fact]
        public void Test_Bad_Impersonate()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.AddAlterationRules(new Rule(new AddRequestHeaderAction("A", "B"), new StatusCodeSuccessFilter()));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting)
                                  .Where(r => r.Level == ValidationRuleLevel.Warning)
                                  .ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Warning, results[0].Level);
        }

        [Fact]
        public void Test_Action_Impersonate()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.AddAlterationRulesForAny(new ImpersonateAction(ImpersonateProfileManager.Chrome131Android));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting)
                                  .Where(r => r.Level == ValidationRuleLevel.Warning)
                                  .ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Warning, results[0].Level);
        }


        [Fact]
        public void Test_Action_Unset_Null_Property()
        {
            var fluxzySetting = FluxzySetting.CreateDefault();

            fluxzySetting.AddAlterationRulesForAny(new AddRequestHeaderAction(null!, "Value"));

            var analyzer = new DefaultFluxzySettingAnalyzer();

            var results = analyzer.Validate(fluxzySetting)
                                  .Where(r => r.Level == ValidationRuleLevel.Error)
                                  .ToList();

            Assert.Single(results);
            Assert.Equal(ValidationRuleLevel.Error, results[0].Level);
        }
    }
}
