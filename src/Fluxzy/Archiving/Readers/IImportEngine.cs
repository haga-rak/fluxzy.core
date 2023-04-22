// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Readers
{
    public interface IImportEngine
    {
        bool IsFormat(string fileName);

        void WriteToDirectory(string fileName, string directory); 
    }
}
