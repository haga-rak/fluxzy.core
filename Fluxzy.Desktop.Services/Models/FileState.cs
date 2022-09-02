using System.Text.Json.Serialization;

namespace Fluxzy.Desktop.Services.Models
{
    public class FileState
    {
        public FileState(Guid identifier, string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            Identifier = identifier;

            ContentOperation = new FileContentOperationManager(this);
        }

        public Guid Identifier { get; set; }

        public string WorkingDirectory { get; set; }

        public string? MappedFileFullPath { get; set; }

        public string ? MappedFileName { get; set; }

        public bool Changed { get; set; }

        public DateTime LastModification { get; set; } = DateTime.Now;

        [JsonIgnore]
        public FileContentOperationManager ContentOperation { get;  }
    }
}