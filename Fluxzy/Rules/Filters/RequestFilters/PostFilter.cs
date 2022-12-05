// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    /// Select POST only url 
    /// </summary>

    [FilterMetaData(
        LongDescription = "Select POST (request method) only exchanges."
    )]
    public class PostFilter : MethodFilter
    {
        public override string GenericName => "POST only";

        public override bool Common { get; set; } = true;

        public override string ShortName => "post";

        public override bool PreMadeFilter => true;

        public PostFilter()
            : base("POST")
        {
        }
    }

    public class PatchFilter : MethodFilter
    {
        public override string GenericName => "PATCH only";

        public override string ShortName => "patch";

        public override bool PreMadeFilter => true;

        public PatchFilter()
            : base("PATCH")
        {
        }
    }

    public class DeleteFilter : MethodFilter
    {
        public override string GenericName => "DELETE only";

        public override string ShortName => "del";

        public override bool PreMadeFilter => true;

        public DeleteFilter()
            : base("DELETE")
        {
        }
    }

    public class PutFilter : MethodFilter
    {
        public override string GenericName => "PUT only";

        public override string ShortName => "put";

        public override bool PreMadeFilter => true;

        public PutFilter()
            : base("PUT")
        {
        }
    }
}
