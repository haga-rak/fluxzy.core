using System.Threading.Tasks;
using Fluxzy.Rules;

namespace Fluxzy.Core
{
    internal class ExchangeContextBuilder : IExchangeContextBuilder
    {
        private readonly ProxyRuntimeSetting _runtimeSetting;

        public ExchangeContextBuilder(ProxyRuntimeSetting runtimeSetting)
        {
            _runtimeSetting = runtimeSetting;
        }

        public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
        {
            var result = new ExchangeContext(authority,
                _runtimeSetting.VariableContext, _runtimeSetting.StartupSetting, 
                _runtimeSetting.ActionMapping) {
                Secure = secure
            };

            return  _runtimeSetting.EnforceRules(result, FilterScope.OnAuthorityReceived); 
        }
    }
}