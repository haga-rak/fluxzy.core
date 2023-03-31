// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class PatchFilter : MethodFilter
    {
        public PatchFilter()
            : base("PATCH")
        {
        }

        public override string GenericName => "PATCH only";

        public override string ShortName => "patch";

        public override bool PreMadeFilter => true;
    }
}
