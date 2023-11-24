// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Readers
{
    /// <summary>
    ///  Defines the expected behavior of an archive import engine
    /// </summary>
    public interface IImportEngine
    {
        bool IsFormat(string fileName);

        void WriteToDirectory(string fileName, string directory); 
    }
}
