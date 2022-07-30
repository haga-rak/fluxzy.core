// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Rules.Filters;

public abstract class HeaderFilter : StringFilter
{
    public string HeaderName { get; set; }
}