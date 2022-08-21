namespace Fluxzy.Desktop.Services
{
    public interface IFileSessionManager
    {
        Task<UiState> New(); 

        Task<IReadOnlyCollection<ExchangeInfo>> ReadExchanges(Guid fileIdentifier, int start, int count);
        
        Task<IReadOnlyCollection<ConnectionInfo>> ReadConnections(Guid fileIdentifier); 

        Task AppendEntries(Guid fileIdentifier,
            IEnumerable<IEchoesSession> sessions); 

        Task RemoveEntries(Guid fileIdentifier,
            IEnumerable<int> sessionId); 
    }

    public enum EchoesFileType
    {
        Error = 0,
        Native = 1 , 
        Har = 5 , 
        Saz = 50 , 
    }

    public class FileState
    {
        public FileState(Guid identifier, string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            Identifier = identifier;
        }

        public Guid Identifier { get; set; }

        public string WorkingDirectory { get; set; }

        public string ?  MappedFile { get; set; }

        public bool Changed { get; set; }

        public DateTime LastModification { get; set; } = DateTime.Now; 
    }
    
    public interface IEchoesSession
    {
        long Id { get;  }

        string Method { get;  }
    }
}