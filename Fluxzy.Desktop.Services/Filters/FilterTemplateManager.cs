using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class FilterTemplateManager
    {
        public List<FilterTemplate> ReadAvailableTemplates()
        {
            var res = new List<FilterTemplate>()
            {
                new FullUrlFilter(string.Empty),
                new HostFilter(string.Empty),
                new MethodFilter(string.Empty),
                new PathFilter(string.Empty),
                new RequestHeaderFilter("Header_name", "Header_value"),

                new ContentTypeJsonFilter(),
                new ContentTypeXmlFilter(),
                new ResponseHeaderFilter("Header_name", "Header_value"),
                new StatusCodeSuccessFilter(),
                new StatusCodeClientErrorFilter(),
                new StatusCodeServerErrorFilter(),
                new StatusCodeFilter(),
                new StatusCodeRedirectionFilter(),

                new AnyFilter(),
                new FilterCollection(),
                new IpEgressFilter("164.132.227.11"),
            };

            return res; 
        }
    }
}
