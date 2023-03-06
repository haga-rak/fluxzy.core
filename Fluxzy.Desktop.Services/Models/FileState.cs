// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using Reinforced.Typings.Attributes;

namespace Fluxzy.Desktop.Services.Models
{
    public class FileState
    {
        private FileState()
        {
        }

        public FileState(FileManager owner, string workingDirectory)
        {
            Owner = owner;
            WorkingDirectory = new DirectoryInfo(workingDirectory).FullName;
            Identifier = WorkingDirectory;
            ContentOperation = new FileContentOperationManager(this);
        }

        public FileState(FileManager owner, string workingDirectory, string mappedFilePullPath)
            : this(owner, workingDirectory)
        {
            var fileInfo = new FileInfo(mappedFilePullPath);
            MappedFileFullPath = fileInfo.FullName;
            MappedFileName = fileInfo.Name;
        }

        [TsIgnore]
        [JsonIgnore]
        public FileManager Owner { get; private init; }

        public string Identifier { get; private init; }

        public string WorkingDirectory { get; private init; }

        public string? MappedFileFullPath { get; private init; }

        public string? MappedFileName { get; private init; }

        public bool Unsaved { get; private init; }

        public DateTime LastModification { get; private init; } = DateTime.Now;

        [JsonIgnore]
        public FileContentOperationManager ContentOperation { get; private init; }

        public FileState SetFileName(string newFileName)
        {
            var fileInfo = new FileInfo(newFileName);

            return new FileState {
                Owner = Owner,
                MappedFileName = fileInfo.Name,
                Unsaved = Unsaved,
                ContentOperation = ContentOperation,
                Identifier = Identifier,
                LastModification = LastModification,
                MappedFileFullPath = fileInfo.FullName,
                WorkingDirectory = WorkingDirectory
            };
        }

        public FileState SetUnsaved(bool state)
        {
            return new FileState {
                Owner = Owner,
                MappedFileName = MappedFileName,
                Unsaved = state,
                ContentOperation = ContentOperation,
                Identifier = Identifier,
                LastModification = LastModification,
                MappedFileFullPath = MappedFileFullPath,
                WorkingDirectory = WorkingDirectory
            };
        }
    }
}
