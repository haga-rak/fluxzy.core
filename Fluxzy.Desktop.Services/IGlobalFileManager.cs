// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public interface IGlobalFileManager
    {
        Task<FileState> New();
        
        Task<FileState> Open(string fileName); 

        Task<FileState> Save(string fileName); 

        Task<FileState> Export(Stream outStream, EchoesFileType fileType); 
    }
}