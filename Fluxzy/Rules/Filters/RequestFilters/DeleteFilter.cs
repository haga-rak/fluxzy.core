// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class DeleteFilter : MethodFilter
    {
        public DeleteFilter()
            : base("DELETE")
        {
        }

        public override string GenericName => "DELETE only";

        public override string ShortName => "del";

        public override bool PreMadeFilter => true;
    }
}
