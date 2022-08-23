namespace Fluxzy.Desktop.Services.Models
{
    public class FileState
    {
        public FileState(Guid identifier, string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            Identifier = identifier;
        }

        public Guid Identifier { get; set; }

        public string WorkingDirectory { get; set; }

        public string? MappedFile { get; set; }

        public bool Changed { get; set; }

        public DateTime LastModification { get; set; } = DateTime.Now;
    }
}