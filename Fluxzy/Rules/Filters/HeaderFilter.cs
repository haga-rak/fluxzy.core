// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters
{
    public abstract class HeaderFilter : StringFilter
    {
        public string HeaderName { get; set; }

        public override string FriendlyName => $"{HeaderName} : {base.FriendlyName}";

    }
}