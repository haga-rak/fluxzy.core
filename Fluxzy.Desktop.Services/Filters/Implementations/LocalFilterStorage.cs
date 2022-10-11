using System.Text.Json;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters.Implementations
{
    public class LocalFilterStorage : IFilterStorage
    {
        private static readonly DirectoryInfo FilterDirectory;

        static LocalFilterStorage()
        {
            var basePath = Environment.ExpandEnvironmentVariables("%appdata%/fluxzy/filters");
            Directory.CreateDirectory(basePath);
            FilterDirectory = new DirectoryInfo(basePath);
        }

        public StoreLocation StoreLocation => StoreLocation.Computer;

        public IEnumerable<Filter> Get()
        {
            foreach (var filterFile in FilterDirectory.EnumerateFiles("*.filter.json"))
            {
                using var stream = filterFile.Open(FileMode.Open, FileAccess.Read);

                var filter = JsonSerializer.Deserialize<Filter>(stream, GlobalArchiveOption.JsonSerializerOptions);

                if (filter != null)
                    yield return filter;
            }
        }

        private static string GetFullPath(Guid filterId)
        {
            return Path.Combine(FilterDirectory.FullName, $"{filterId}.filter.json");
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
                filter = JsonSerializer.Deserialize<Filter>(fullPath, GlobalArchiveOption.JsonSerializerOptions);
                return true;
            }

            filter = null;
            return false;
        }

        public void AddOrUpdate(Guid filterId, Filter updatedContent)
        {
            var fullPath = GetFullPath(filterId);

            using var outStream = File.Create(fullPath);
            JsonSerializer.Serialize(outStream, updatedContent, GlobalArchiveOption.JsonSerializerOptions);
        }
    }
}