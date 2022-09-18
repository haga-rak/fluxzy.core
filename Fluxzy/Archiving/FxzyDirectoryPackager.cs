// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fluxzy
{
    public class FxzyDirectoryPackager : IDirectoryPackager
    {
        public bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".fxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fluxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fxzy.zip", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fluxzy.zip", StringComparison.CurrentCultureIgnoreCase) ; 
        }

        public async Task Unpack(Stream inputStream, string directoryOutput)
        {
            await ZipHelper.Decompress(inputStream, new DirectoryInfo(directoryOutput)); 
        }

        public async Task Pack(string directory, Stream outputStream) 
        {
            await ZipHelper.Compress(new DirectoryInfo(directory),
                outputStream, fileInfo =>
                {
                    if (fileInfo.Length == 0)
                        return false;

                    if (fileInfo.Name.EndsWith(".data") 
                        || fileInfo.Name.EndsWith(".json") 
                        || fileInfo.Name.EndsWith(".pcap"))
                    {
                        return true; 
                    }

                    return true; 
                }); 
        }
        
        public async Task Pack(string directory, Stream outputStream, 
            IEnumerable<ExchangeInfo> exchangeInfos,
            IEnumerable<ConnectionInfo> connectionInfos)
        {
            var fileInfos = new List<FileInfo>(); 

            foreach (var exchangeInfo in exchangeInfos)
            {
                fileInfos.Add(new FileInfo(DirectoryArchiveHelper.GetExchangePath(directory, exchangeInfo)));
                fileInfos.Add(new FileInfo(DirectoryArchiveHelper.GetContentRequestPath(directory, exchangeInfo)));
                fileInfos.Add(new FileInfo(DirectoryArchiveHelper.GetContentResponsePath(directory, exchangeInfo)));
            }

            foreach (var connectionInfo in connectionInfos)
            {
                fileInfos.Add(new FileInfo(DirectoryArchiveHelper.GetConnectionPath(directory, connectionInfo)));

            }

            await ZipHelper.CompressWithFileInfos(new DirectoryInfo(directory), outputStream, fileInfos);
        }
    }
}