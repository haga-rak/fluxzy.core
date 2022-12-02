using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Desktop.Services.Filters.Implementations
{
    public class LocalFilterStorage : IFilterStorage
    {
        private readonly DirectoryInfo _filterDirectory;

        public LocalFilterStorage()
        {
            var basePath = Environment.ExpandEnvironmentVariables("%appdata%/fluxzy/filters");

            Directory.CreateDirectory(basePath);
            _filterDirectory = new DirectoryInfo(basePath);

            if (!_filterDirectory.EnumerateFiles("*.filter.json").Any())
            {
                // Dump default filters, TODO : add more default filters 

                InternalAdd(AnyFilter.Default); 
                InternalAdd(new MethodFilter("POST") { Locked = true }); 
                InternalAdd(new ContentTypeJsonFilter() { Locked = true, }); 
                InternalAdd(new HostFilter("www.fluxzy.io") { Locked = false}); 
                InternalAdd(new FilterCollection()
                {
                    Children = new List<Filter>()
                    {
                        new HostFilter("msdn.com"),
                        new MethodFilter("PATCH"),
                        new FullUrlFilter("https://github.com/haga-rak/fluxzy/actions")
                    }
                });
            }
        }

        public StoreLocation StoreLocation => StoreLocation.Computer;

        public IEnumerable<Filter> Get()
        {
            foreach (var filterFile in _filterDirectory.EnumerateFiles("*.filter.json"))
            {
                using var stream = filterFile.Open(FileMode.Open, FileAccess.Read);

                var filter = JsonSerializer.Deserialize<Filter>(stream, GlobalArchiveOption.DefaultSerializerOptions);

                if (filter != null)
                    yield return filter;
            }
        }

        private string GetFullPath(Guid filterId)
        {
            return Path.Combine(_filterDirectory.FullName, $"{filterId}.filter.json");
        }

        public bool Remove(Guid filterId)
        {
            var fullPath = GetFullPath(filterId);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }

        public bool TryGet(Guid filterId, out Filter? filter)
        {
            var fullPath = GetFullPath(filterId);

            if (File.Exists(fullPath))
            {
                filter = JsonSerializer.Deserialize<Filter>(fullPath, GlobalArchiveOption.DefaultSerializerOptions);
                return true;
            }

            filter = null;
            return false;
        }

        private void InternalAdd(Filter updatedContent)
        {
            var fullPath = GetFullPath(updatedContent.Identifier);

            using var outStream = File.Create(fullPath);
            JsonSerializer.Serialize(outStream, updatedContent, options : GlobalArchiveOption.DefaultSerializerOptions);
        }

        public void AddOrUpdate(Guid filterId, Filter updatedContent)
        {
            var fullPath = GetFullPath(filterId);

            using var outStream = File.Create(fullPath);
            JsonSerializer.Serialize(outStream, updatedContent, GlobalArchiveOption.DefaultSerializerOptions);
        }

        public void Patch(IEnumerable<Filter> filters)
        {
            // var clear directory 
            foreach (var fileInfo in _filterDirectory.EnumerateFiles("*.filter.json"))
            {
                fileInfo.Delete();
            }

            foreach (var filter in filters)
            {
                AddOrUpdate(filter.Identifier, filter);
            }
        }
    }
}