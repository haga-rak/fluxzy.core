using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Rules.Filters.ViewOnlyFilters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class FilterTemplateManager
    {
        public List<FilterTemplate> ReadAvailableTemplates()
        {
            var res = new List<FilterTemplate>
            {
                new FullUrlFilter(string.Empty),
                new HostFilter(string.Empty),
                new MethodFilter(string.Empty),
                new PathFilter(string.Empty),
                new RequestHeaderFilter("Header_name", "Header_value"),

                new PostFilter(),
                new PatchFilter(),
                new PutFilter(),
                new DeleteFilter(),

                new ContentTypeJsonFilter(),
                new ContentTypeXmlFilter(),
                new ImageFilter(),
                new ResponseHeaderFilter("Header_name", "Header_value"),
                new StatusCodeSuccessFilter(),
                new StatusCodeClientErrorFilter(),
                new StatusCodeServerErrorFilter(),
                new StatusCodeFilter(),
                new StatusCodeRedirectionFilter(),

                new AnyFilter(),
                new FilterCollection
                {
                    Common = true
                },
                new IpEgressFilter("164.132.227.11"),
                new H11TrafficOnlyFilter(),
                new H2TrafficOnlyFilter(),
                new IsWebSocketFilter(),
                new HasRequestBodyFilter(),

                new HasCommentFilter(),
                new HasTagFilter(),
                new CommentSearchFilter(string.Empty),
                new TagContainsFilter(null),
                new AgentLabelFilter(""),
                new SearchTextFilter("")
            };

            return res;
        }
    }
}
