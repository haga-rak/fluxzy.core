// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fluxzy.Desktop.Services.Attributes;

namespace Fluxzy.Desktop.Services.Ui
{
    public class LastOpenFileManager : ObservableProvider<LastOpenFileState>
    {
        private static readonly int MaxFileOpenHistoryCount = 10;

        private readonly string _filePath;

        public LastOpenFileManager()
        {
            var basePath = Environment.ExpandEnvironmentVariables("%appdata%/Fluxzy.Desktop");
            Directory.CreateDirectory(basePath);
            _filePath = Path.Combine(basePath, "settings.last-open-files.json");

            var subject = new BehaviorSubject<LastOpenFileState>(InternalGet());

            subject
                .Do(InternalUpdate)
                .Subscribe();

            Subject = subject;
        }

        protected override BehaviorSubject<LastOpenFileState> Subject { get; }

        private LastOpenFileState InternalGet()
        {
            if (!File.Exists(_filePath))
                return new LastOpenFileState(new List<LastOpenFileItem>());

            return JsonSerializer.Deserialize<LastOpenFileState>(File.ReadAllText(_filePath),
                GlobalArchiveOption.DefaultSerializerOptions)!;
        }

        public void Add(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var list = Subject.Value.Items.ToList();

            list.RemoveAll(i => i.FullPath == fileInfo.FullName);

            list.Insert(0, new LastOpenFileItem(fileInfo));

            while (list.Count > MaxFileOpenHistoryCount) {
                list.RemoveAt(list.Count - 1);
            }

            var nextState = new LastOpenFileState(list);

            InternalUpdate(nextState);
            Subject.OnNext(nextState);
        }

        private void InternalUpdate(LastOpenFileState state)
        {
            File.WriteAllText(_filePath,
                JsonSerializer.Serialize(state, GlobalArchiveOption.DefaultSerializerOptions));
        }
    }

    [Exportable]
    public class LastOpenFileState
    {
        public LastOpenFileState(List<LastOpenFileItem> items)
        {
            Items = items;
        }

        public List<LastOpenFileItem> Items { get; }
    }

    [Exportable]
    public class LastOpenFileItem
    {
        public LastOpenFileItem(FileInfo fileInfo)
        {
            FileName = fileInfo.Name;
            FullPath = fileInfo.FullName;
        }

        [JsonConstructor]
        public LastOpenFileItem(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
        }

        public string FullPath { get; }

        public string FileName { get; }

        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}
