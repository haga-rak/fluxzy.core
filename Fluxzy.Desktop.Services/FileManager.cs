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

        public Task New()
        {
            var newFileState = CreateNewFileState(_tempDirectory);

            Subject.OnNext(newFileState);

            return Task.CompletedTask; 
        }

        private static FileState CreateNewFileState(string tempDirectory)
        {
            var (id, fullPath) = GenerateNewDirectory(tempDirectory);
            var newFileState = new FileState(id, fullPath);
            return newFileState;
        }

        public async Task Open(string fileName)
        {
            var (id, fullPath) = GenerateNewDirectory(_tempDirectory);

            var directoryInfo = new DirectoryInfo(fullPath);

            var openFileInfo = new FileInfo(fileName);

            using var fileStream = openFileInfo.OpenRead();

            await _directoryPackager.Unpack(fileStream, directoryInfo.FullName);

            var result = new FileState(id, fullPath)
            {
                MappedFileFullPath = fileName,
                MappedFileName = openFileInfo.Name
            };

            Subject.OnNext(result);
        }
        
        public async Task Save(string fileName)
        {
            var current = await Observable.FirstAsync(); 

            if (current == null)
                throw new InvalidOperationException("Current working directory/file is not set");

            var outStream = File.Create(fileName);
            
            await _directoryPackager.Pack(current.WorkingDirectory,
                outStream
            );

            var newInstance = current;

            newInstance.WorkingDirectory = fileName;
            newInstance.Changed = false;

            Subject.OnNext(current);

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