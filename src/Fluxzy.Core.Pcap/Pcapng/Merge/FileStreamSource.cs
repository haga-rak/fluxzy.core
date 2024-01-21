namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    /// <summary>
    ///     A classic stream from an existing file
    /// </summary>
    public class FileStreamSource : IStreamSource
    {
        private readonly string _fileName;

        public FileStreamSource(string fileName)
        {
            _fileName = fileName;
        }

        public Stream Open()
        {
            return File.OpenRead(_fileName);
        }
    }
}