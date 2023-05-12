// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    ///     Select exchange that has request body.
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select request having body."
    )]
    public class HasRequestBodyFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.ResponseBodyReceivedFromRemote;

        public override string GenericName => "Has Request body";

        public override string ShortName => "req body.";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return filteringContext?.HasRequestBody ?? false;
        }
    }
}
