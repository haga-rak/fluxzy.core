using System.Threading.Tasks;

namespace Fluxzy.Core
{
    internal interface IExchangeContextBuilder
    {
        ValueTask<ExchangeContext> Create(Authority authority, bool secure);
    }
}