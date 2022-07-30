using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations;

public class PathFilter : StringFilter
{
    protected override IEnumerable<string> GetMatchInput(Exchange exchange)
    {
        yield return exchange.Request.Header.Path.ToString();
    }
}