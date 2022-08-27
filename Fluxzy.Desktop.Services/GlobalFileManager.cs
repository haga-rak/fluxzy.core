// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class GlobalFileManager
    {
        private readonly FxzyDirectoryPackager _directoryPackager;
        private readonly string _tempDirectory;

        public GlobalFileManager(IConfiguration configuration, FxzyDirectoryPackager directoryPackager)
        {
            _directoryPackager = directoryPackager;
            _tempDirectory = configuration["UiSettings:CaptureTemp"]
                             ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                 "Fluxzy.Desktop", "temp");
            _tempDirectory = Environment.ExpandEnvironmentVariables(_tempDirectory);

            Directory.CreateDirectory(_tempDirectory); 
        }

        public FileState? Current { get; private set; } = null;

        private (Guid,string) GenerateNewDirectory()
        {
            var id = Guid.NewGuid(); 

            var pathSuffix = $"capture-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{id}";

            var fullPath = Path.Combine(_tempDirectory, pathSuffix);

            Directory.CreateDirectory(fullPath);
            return (id, fullPath);
        }

        public Task<FileState> New()
        {
            var (id, fullPath) = GenerateNewDirectory();

            Current = new FileState(id, fullPath);

            return Task.FromResult(Current); 
        }

        public async Task<FileState> Open(string fileName)
        {
            var (id, fullPath) = GenerateNewDirectory();

            var directoryInfo = new DirectoryInfo(fullPath);

            var openFileInfo = new FileInfo(fileName);

            using var fileStream = openFileInfo.OpenRead();

            await _directoryPackager.Unpack(fileStream, directoryInfo.FullName);

            var result = Current = new FileState(id, fullPath)
            {
                MappedFileFullPath = fileName,
                MappedFileName = openFileInfo.Name
            };

            return result; 
        }
        
        public async Task<FileState> Save(string fileName)
        {
            if (Current == null)
                throw new InvalidOperationException("Current working directory/file is not set");

            var outStream = File.Create(fileName);
            
            await _directoryPackager.Pack(Current.WorkingDirectory,
                outStream
            );

            Current.WorkingDirectory = fileName;
            Current.Changed = false; 

            return Current;

        }

        public Task<FileState> Export(Stream outStream, FluxzyFileType fileType)
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