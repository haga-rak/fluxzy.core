using Fluxzy.Rules.Filters.RequestFilters;

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
            };

            return res; 
        }
    }
}
