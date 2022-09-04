// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.Configuration;


namespace Fluxzy.Desktop.Services
{
    public class FileManager : ObservableProvider<FileState>
    {
        private readonly FxzyDirectoryPackager _directoryPackager;
        private readonly string _tempDirectory;

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
        
        public sealed override BehaviorSubject<FileState> Subject { get; }
        
        private static (Guid,string) GenerateNewDirectory(string tempDirectory)
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

            using var fileStream = openFileInfo.OpenRead();

            await _directoryPackager.Unpack(fileStream, directoryInfo.FullName);

            var result = new FileState(this, workingDirectory, fileName); 

            Subject.OnNext(result);
        }

        public void SetUnsaved(bool state)
        {
            Subject.OnNext(Subject.Value.SetUnsaved(state)); 
        }

        public async Task Save(TrunkState trunkState)
        {
            var current = await Observable.FirstAsync(); 

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
            var current = await Observable.FirstAsync(); 

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

        public Task Export(Stream outStream, FluxzyFileType fileType)
        {
            throw new NotImplementedException();
        }
    }

    public enum FluxzyFileType
    {
        Error = 0,
        Native = 1,
        Har = 5,
        Saz = 50,
    }

}