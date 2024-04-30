namespace Fluxzy.Clipboard
{
    public class CopyArtefact
    {
        public CopyArtefact(string path, string extension, byte[]? binary, string? filePath)
        {
            Path = path;
            Extension = extension;
            Binary = binary;
            FilePath = filePath;
        }

        public string Path { get; }

        public string Extension { get; }

        public byte[]? Binary { get; }

        public string? FilePath { get; }
    }
}