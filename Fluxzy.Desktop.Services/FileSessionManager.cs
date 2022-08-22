using System.Text.Json;

namespace Fluxzy.Desktop.Services
{
    public class FileSessionManager
    {
        private readonly GlobalFileManager _globalFileManager;

        public FileSessionManager(GlobalFileManager globalFileManager)
        {
            _globalFileManager = globalFileManager;
        }

        public Task<int> ExchangeCount()
        {
            var current = _globalFileManager.Current;

            if (current == null)
                return Task.FromResult(0);

            var count =
                new DirectoryInfo(Path.Combine(current.WorkingDirectory, "exchanges"))
                    .EnumerateFiles("*.json").Count();

            return Task.FromResult(count);
        }

        public async IAsyncEnumerable<ExchangeInfo> ReadExchanges(int start, int count)
        {
            var current = _globalFileManager.Current;

            if (current == null)
                yield break;

            var fileInfos =
                new DirectoryInfo(Path.Combine(current.WorkingDirectory, "exchanges"))
                    .EnumerateFiles("*.json")
                    .OrderBy(o => o.Name)
                    .Skip(start)
                    .Take(count);

            foreach (var fileInfo in fileInfos)
            {
                ExchangeInfo ? exchange = null; 
                try
                {
                    exchange = await JsonSerializer.DeserializeAsync<ExchangeInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is writing to file )
                    continue; 
                }

                if (exchange != null)
                    yield return exchange;
            }
        }

        public async IAsyncEnumerable<ConnectionInfo> ReadConnections()
        {
            var current = _globalFileManager.Current;

            if (current == null)
                yield break;

            var fileInfos =
                new DirectoryInfo(Path.Combine(current.WorkingDirectory, "connections"))
                    .EnumerateFiles("*.json")
                    .OrderBy(o => o.Name); 

            foreach (var fileInfo in fileInfos)
            {
                ConnectionInfo? connection = null;
                try
                {
                    connection = await JsonSerializer.DeserializeAsync<ConnectionInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is writing to file )
                    continue;
                }

                if (connection != null)
                    yield return connection;
            }
        }
    }
    
}