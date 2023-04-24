// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    ///     Select POST only url
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select POST (request method) only exchanges."
    )]
    public class PostFilter : MethodFilter
    {
        public PostFilter()
            : base("POST")
        {
        }

        public override string GenericName => "POST only";

        public override bool Common { get; set; } = true;

        public override string ShortName => "post";

        public override bool PreMadeFilter => true;
    }

    public class PutFilter : MethodFilter
    {
        public PutFilter()
            : base("PUT")
        {
        }

        public override string GenericName => "PUT only";

        public override string ShortName => "put";

        public override bool PreMadeFilter => true;
    }
}