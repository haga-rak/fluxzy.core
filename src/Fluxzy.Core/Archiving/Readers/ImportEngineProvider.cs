// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fluxzy.Readers
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportEngineProvider
    {
        public ImportEngineProvider(FxzyDirectoryPackager directoryPackager)
        {
            Engines = new ReadOnlyCollection<IImportEngine>(new List<IImportEngine>() {
                new FxzyImportEngine(directoryPackager),
                new SazImportEngine(),
                new HarImportEngine(),
            }); 
        }

        public IReadOnlyCollection<IImportEngine> Engines { get; }

        public IImportEngine? GetImportEngine(string fileName)
        {
            return Engines.FirstOrDefault(r => r.IsFormat(fileName));
        }
    }
}
