// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Ui.Logging
{
    static class UidProvider
    {
        private static readonly object _lock = new();
        private static string? _current; 

        public static string Current()
        {
            if (_current != null)
                return _current;

            lock (_lock) {

                var newId = Guid.NewGuid().ToString();

                var fullDirectory = Environment.ExpandEnvironmentVariables("%appdata%/Fluxzy.Desktop");
                Directory.CreateDirectory(fullDirectory); 

                var path = Path.Combine(fullDirectory, "uid.txt");

                if (!File.Exists(path) || !Guid.TryParse(newId = File.ReadAllText(path), out _)) {
                    File.WriteAllText(path, newId);
                }

                return _current = newId;
            }
        }
    }
}
