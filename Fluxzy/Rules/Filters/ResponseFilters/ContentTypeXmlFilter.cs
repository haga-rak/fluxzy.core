namespace Echoes.Rules.Filters.ResponseFilters
{
    public class ContentTypeXmlFilter : ResponseHeaderFilter
    {
        public ContentTypeXmlFilter()
        {
            HeaderName = "Content-Type";
            Pattern = "xml";
            CaseSensitive = false;
            Operation = StringSelectorOperation.Contains;
        }

        public override string FriendlyName { get; } = "XML response only";
    }
}