namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class ContentTypeJsonFilter : ResponseHeaderFilter
    {
        public override string FriendlyName { get; } = "JSON response only";
        
        public ContentTypeJsonFilter() : base("json", StringSelectorOperation.Contains, "Content-Type")
        {
        }
    }
}