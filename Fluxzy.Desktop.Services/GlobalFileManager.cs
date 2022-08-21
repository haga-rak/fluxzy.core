﻿// Copyright © 2022 Haga Rakotoharivelo

using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class GlobalFileManager : IGlobalFileManager
    {
        private FileState? _current = null;

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

            _current = new FileState(id, fullPath);

            return Task.FromResult(_current); 
        }

        public async Task<FileState> Open(string fileName)
        {
            var (id, fullPath) = GenerateNewDirectory();

            var directoryInfo = new DirectoryInfo(fullPath); 

            using var fileStream = File.OpenRead(fileName);

            await _directoryPackager.Unpack(fileStream, directoryInfo.FullName);

            var result = new FileState(id, fullPath)
            {
                MappedFile = directoryInfo.FullName,
            };

            return result; 
        }

        public async Task<FileState> Save(string fileName)
        {
            if (_current == null)
                throw new InvalidOperationException("Current working directory/file is not set");

            var outStream = File.Create(fileName);
            
            await _directoryPackager.Pack(_current.WorkingDirectory,
                outStream
            );

            _current.WorkingDirectory = fileName;
            _current.Changed = false; 

            return _current;

        }

        public Task<FileState> Export(Stream outStream, EchoesFileType fileType)
        {
            throw new NotImplementedException();
        }
    }
}