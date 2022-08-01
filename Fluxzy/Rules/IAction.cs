// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public interface IAction
    {
        FilterScope ActionScope { get; }

        Task Alter(ExchangeContext context, 
                Exchange exchange,
                Connection connection);
    }
}