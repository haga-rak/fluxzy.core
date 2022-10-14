namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class ContentTypeJsonFilter : ResponseHeaderFilter
    {
        public override string FriendlyName { get; } = "JSON response only";
        
        public ContentTypeJsonFilter() : base("json", StringSelectorOperation.Contains, "Content-Type")
        {
        }

        public override string GenericName => "JSON response only";

        public override bool PreMadeFilter => true;
    }
}