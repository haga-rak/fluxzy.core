// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters.RequestFilters
{
    [FilterMetaData(
        LongDescription = "Select exchanges with GET method"
    )]
    public class GetFilter : MethodFilter
    {
        public GetFilter()
            : base("GET")
        {

        }

        public override string GenericName => "GET only";

        public override string ShortName => "patch";

        public override bool PreMadeFilter => true;
    }
}
