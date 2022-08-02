namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class ContentTypeXmlFilter : ResponseHeaderFilter
    {
        public override string FriendlyName { get; } = "XML response only";
        
        public ContentTypeXmlFilter() : base("xml", StringSelectorOperation.Contains, "Content-Type")
        {
        }
    }
}