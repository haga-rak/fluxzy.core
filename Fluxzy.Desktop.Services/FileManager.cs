// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Har;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FileManager : ObservableProvider<FileState>
    {
        private readonly FxzyDirectoryPackager _directoryPackager;
        private readonly string _tempDirectory;

        protected sealed override BehaviorSubject<FileState> Subject { get; }

        public override IObservable<FileState> ProvidedObservable => Subject.AsObservable().DistinctUntilChanged();

        public FileManager(IConfiguration configuration, FxzyDirectoryPackager directoryPackager)
        {
            _directoryPackager = directoryPackager;

            _tempDirectory = configuration["UiSettings:CaptureTemp"]
                             ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                 "Fluxzy.Desktop", "temp");

            _tempDirectory = Environment.ExpandEnvironmentVariables(_tempDirectory);

            Directory.CreateDirectory(_tempDirectory);

            Subject = new BehaviorSubject<FileState>(CreateNewFileState(_tempDirectory));
        }

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

        public async Task Open(string fileName)
        {
            var (_, workingDirectory) = GenerateNewDirectory(_tempDirectory);

            var directoryInfo = new DirectoryInfo(workingDirectory);

            var openFileInfo = new FileInfo(fileName);

            await using var fileStream = openFileInfo.OpenRead();

            await _directoryPackager.Unpack(fileStream, directoryInfo.FullName);

            var result = new FileState(this, workingDirectory, fileName);

            Subject.OnNext(result);
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

            await _directoryPackager.Pack(current.WorkingDirectory, outStream,
                trunkState.Exchanges.Select(e => e.ExchangeInfo),
                trunkState.Connections.Select(c => c.ConnectionInfo));

            var nextState = current.SetUnsaved(false);

            Subject.OnNext(nextState);
        }

        public async Task SaveAs(TrunkState trunkState, string fileName)
        {
            var current = await ProvidedObservable.FirstAsync();

            if (current == null)
                throw new InvalidOperationException("Current working directory/file is not set");

            using var outStream = File.Create(fileName);

            await _directoryPackager.Pack(current.WorkingDirectory, outStream,
                trunkState.Exchanges.Select(e => e.ExchangeInfo),
                trunkState.Connections.Select(c => c.ConnectionInfo));

            var nextState = current
                            .SetFileName(fileName)
                            .SetUnsaved(false);

            Subject.OnNext(nextState);
        }
        
        public async Task<bool> ExportHttpArchive(HarExportRequest exportRequest)
        {
            var current = await ProvidedObservable.FirstAsync();
            var harArchive = new HttpArchivePackager(exportRequest.SaveSetting);

            // read exchanges 

            

            

            using var stream = File.Create(exportRequest.FileName);


            

            

            harArchive.Pack(current.WorkingDirectory, stream, TODO); 
        }
    }

    public enum FluxzyFileType
    {
        Har = 5,
        Saz = 50
    }


    public class HarExportRequest
    {
        public HarExportRequest(string fileName, HttpArchiveSavingSetting saveSetting, List<int> exchangeIds)
        {
            FileName = fileName;
            SaveSetting = saveSetting;
            ExchangeIds = exchangeIds;
        }

        public string FileName { get;  }

        public HttpArchiveSavingSetting SaveSetting { get;  }

        public List<int> ExchangeIds { get; }
    }
}
