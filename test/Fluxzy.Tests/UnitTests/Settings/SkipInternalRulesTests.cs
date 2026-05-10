using System.Linq;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Settings
{
    public class SkipInternalRulesTests
    {
        [Fact]
        public void Default_Includes_Welcome_And_Ca_Mounts()
        {
            var setting = FluxzySetting.CreateDefault();

            var actionTypes = setting.FixedRules().Select(r => r.Action.GetType()).ToList();

            Assert.Contains(typeof(MountCertificateAuthorityAction), actionTypes);
            Assert.Contains(typeof(MountWelcomePageAction), actionTypes);
        }

        [Fact]
        public void SkipInternalRules_Removes_Welcome_And_Ca_Mounts()
        {
            var setting = FluxzySetting.CreateDefault().SetSkipInternalRules(true);

            var actionTypes = setting.FixedRules().Select(r => r.Action.GetType()).ToList();

            Assert.DoesNotContain(typeof(MountCertificateAuthorityAction), actionTypes);
            Assert.DoesNotContain(typeof(MountWelcomePageAction), actionTypes);
        }

        [Fact]
        public void SkipInternalRules_Preserves_GlobalSkipSslDecryption_Rule()
        {
            var setting = FluxzySetting.CreateDefault()
                                       .SetSkipInternalRules(true)
                                       .SetSkipGlobalSslDecryption(true);

            var actionTypes = setting.FixedRules().Select(r => r.Action.GetType()).ToList();

            Assert.Contains(typeof(SkipSslTunnelingAction), actionTypes);
            Assert.DoesNotContain(typeof(MountCertificateAuthorityAction), actionTypes);
            Assert.DoesNotContain(typeof(MountWelcomePageAction), actionTypes);
        }
    }
}
