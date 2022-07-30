namespace Echoes.Rules.Filters.ResponseFilters
{
    public class ContentTypeJsonFilter : ResponseHeaderFilter
    {
        public ContentTypeJsonFilter()
        {
            HeaderName = "Content-Type";
            Pattern = "json";
            CaseSensitive = false;
            Operation = StringSelectorOperation.Contains;
        }

        public override string FriendlyName { get; } = "JSON response only";
    }
}