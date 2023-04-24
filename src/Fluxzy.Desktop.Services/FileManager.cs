// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Har;
using Fluxzy.Readers;
using Fluxzy.Saz;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FileManager : ObservableProvider<FileState>
    {
        private readonly FxzyDirectoryPackager _directoryPackager;
        private readonly ImportEngineProvider _importEngineProvider;

        private readonly string _tempDirectory;

        public FileManager(
            IConfiguration configuration, FxzyDirectoryPackager directoryPackager,
            ImportEngineProvider importEngineProvider)
        {
            _directoryPackager = directoryPackager;
            _importEngineProvider = importEngineProvider;

            _tempDirectory = configuration["UiSettings:CaptureTemp"]
                             ??
                             Environment.ExpandEnvironmentVariables(
                                 "%appdata%/Fluxzy.Desktop/temp/expand");

            _tempDirectory = Environment.ExpandEnvironmentVariables(_tempDirectory);

            Directory.CreateDirectory(_tempDirectory);

            Subject = new BehaviorSubject<FileState>(CreateNewFileState(_tempDirectory));

            string? previous = null;

            Subject.AsObservable()
                   .Select(s => s.WorkingDirectory)
                   .DistinctUntilChanged()
                   .Do(current => {
                           if (previous != null) {
                               var prev = previous; 
                               Task.Run(() => {
                                   // fire and forget WorkingDirectory deletion 
                                   try {
                                       Directory.Delete(prev, true);
                                   }
                                   catch {
                                       // Ignore delete errors
                                   }
                               });
                           }
                           previous = current;
                       }
                   )
                   .Subscribe();
        }

        protected sealed override BehaviorSubject<FileState> Subject { get; }

        public override IObservable<FileState> ProvidedObservable => Subject.AsObservable().DistinctUntilChanged();

        private static (Guid, string) GenerateNewDirectory(string tempDirectory)
        {
            var id = Guid.NewGuid();

            var pathSuffix = $"capture-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{id}";

            var fullPath = Path.Combine(tempDirectory, pathSuffix);

            Directory.CreateDirectory(fullPath);

            return (id, fullPath);
        }

        private FileState CreateNewFileState(string tempDirectory)
        {
            var (_, fullPath) = GenerateNewDirectory(tempDirectory);
            var newFileState = new FileState(this, fullPath);

            return newFileState;
        }

        public Task New()
        {
            var newFileState = CreateNewFileState(_tempDirectory);

            Subject.OnNext(newFileState);

            return Task.CompletedTask;
        }

        public Task Open(string fileName)
        {
            var (_, workingDirectory) = GenerateNewDirectory(_tempDirectory);

            var directoryInfo = new DirectoryInfo(workingDirectory);

            var openFileInfo = new FileInfo(fileName);

            var importEngine = _importEngineProvider.GetImportEngine(fileName);

            if (importEngine == null)
                throw new InvalidOperationException("No import engine found for file");

            importEngine.WriteToDirectory(openFileInfo.FullName, directoryInfo.FullName);

            if (importEngine is FxzyImportEngine) {
                // set open file name 
                var result = new FileState(this, workingDirectory, fileName);
                Subject.OnNext(result);
            }
            else {
                var result = new FileState(this, workingDirectory);
                Subject.OnNext(result);
            }

            return Task.CompletedTask;
        }

        public void SetUnsaved(bool state)
        {
            if (Subject.Value.Unsaved != state)
                Subject.OnNext(Subject.Value.SetUnsaved(state));
        }

        public async Task Save(TrunkState trunkState)
        {
            var current = await ProvidedObservable.FirstAsync();

            if (current.MappedFileFullPath == null)
                throw new InvalidOperationException("No mapped filed");

            using var outStream = File.Create(current.MappedFileFullPath);

            var exchangeIds = trunkState.Exchanges.Select(s => s.Id).ToHashSet();

            await _directoryPackager.Pack(current.WorkingDirectory, outStream, exchangeIds);

            var nextState = current.SetUnsaved(false);

            Subject.OnNext(nextState);
        }

        public async Task SaveAs(TrunkState trunkState, string fileName)
        {
            var current = await ProvidedObservable.FirstAsync();

            if (current == null)
                throw new InvalidOperationException("Current working directory/file is not set");

            using var outStream = File.Create(fileName);

            var exchangeIds = trunkState.Exchanges.Select(s => s.Id).ToHashSet();

            await _directoryPackager.Pack(current.WorkingDirectory, outStream, exchangeIds);

            var nextState = current
                            .SetFileName(fileName)
                            .SetUnsaved(false);

            Subject.OnNext(nextState);
        }

        public async Task<bool> ExportHttpArchive(HarExportRequest exportRequest)
        {
            var current = await ProvidedObservable.FirstAsync();
            var exchangeIds = exportRequest.ExchangeIds?.ToHashSet();

            await using var stream = File.Create(exportRequest.FileName);
            var harArchive = new HttpArchivePackager(exportRequest.SaveSetting);
            await harArchive.Pack(current.WorkingDirectory, stream, exchangeIds);

            return true;
        }

        public async Task<bool> ExportSaz(SazExportRequest exportRequest)
        {
            var current = await ProvidedObservable.FirstAsync();
            var exchangeIds = exportRequest.ExchangeIds?.ToHashSet();
            await using var stream = File.Create(exportRequest.FileName);

            var sazPackager = new SazPackager();
            await sazPackager.Pack(current.WorkingDirectory, stream, exchangeIds);

            return true;
        }
    }

    public class HarExportRequest
    {
        public HarExportRequest(string fileName, HttpArchiveSavingSetting saveSetting, List<int>? exchangeIds)
        {
            FileName = fileName;
            SaveSetting = saveSetting;
            ExchangeIds = exchangeIds;
        }

        public string FileName { get; }

        public HttpArchiveSavingSetting SaveSetting { get; }

        public List<int>? ExchangeIds { get; }
    }

    public class SazExportRequest
    {
        public SazExportRequest(string fileName, List<int>? exchangeIds)
        {
            FileName = fileName;
            ExchangeIds = exchangeIds;
        }

        public string FileName { get; }

        public List<int>? ExchangeIds { get; }
    }
}