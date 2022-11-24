// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Har
{
    [PackagerInformation("har", "HAR 1.2 archive format", ".har")]
    public class HttpArchivePackager : IDirectoryPackager
    {
        private readonly HttpArchiveSavingSetting _savingSetting;

        public HttpArchivePackager(HttpArchiveSavingSetting?  savingSetting = null)
        {
            _savingSetting = savingSetting ?? HttpArchiveSavingSetting.Default;
        }

        public bool ShouldApplyTo(string fileName)
        {
            return fileName.EndsWith(".har", StringComparison.CurrentCultureIgnoreCase);
        }

        public Task Pack(string directory, Stream outputStream)
        {
            var exchanges =
                DirectoryArchiveHelper.EnumerateExchangeFileCandidates(directory)
                                      .Select(fileInfo =>
                                      {
                                          try
                                          {
                                              using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read,
                                                  FileShare.ReadWrite);

                                              var current = JsonSerializer.Deserialize<ExchangeInfo>(stream,
                                                  GlobalArchiveOption.DefaultSerializerOptions);

                                              return current;
                                          }
                                          catch
                                          {
                                              // We suppress all reading warning here caused by potential pending reads 
                                              // TODO : think of a better way 

                                              return null;
                                          }
                                      }).Where(e => e != null).OfType<ExchangeInfo>();

            var connections =
                DirectoryArchiveHelper.EnumerateConnectionFileCandidates(directory)
                                      .Select(fileInfo =>
                                      {
                                          try
                                          {
                                              using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read,
                                                  FileShare.ReadWrite);

                                              var current = JsonSerializer.Deserialize<ConnectionInfo>(stream,
                                                  GlobalArchiveOption.DefaultSerializerOptions);

                                              return current;
                                          }
                                          catch
                                          {
                                              // We suppress all reading warning here caused by potential pending reads 
                                              // TODO : think of a better way 

                                              return null;
                                          }
                                      })
                                      .Where(e => e != null).OfType<ConnectionInfo>();

            return Pack(directory, outputStream, exchanges, connections);
        }

        public Task Pack(string directory, Stream outputStream,
            IEnumerable<ExchangeInfo> exchangeInfos,
            IEnumerable<ConnectionInfo> connectionInfos)
        {
            var directoryArchiveReader = new DirectoryArchiveReader(directory);

            var harLogModel = new HarLogModel(directoryArchiveReader, exchangeInfos,
                connectionInfos.ToDictionary(t => t.Id, t => t), _savingSetting);

            JsonSerializer.Serialize(outputStream, new HarSerializeRootModel(harLogModel),
                GlobalArchiveOption.HttpArchiveSerializerOptions);

            return Task.CompletedTask;
        }
    }
}
