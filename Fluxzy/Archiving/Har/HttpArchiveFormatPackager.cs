using Fluxzy.Har;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Readers;

namespace Fluxzy.Archiving.Har
{
    public class HttpArchiveFormatPackager : IDirectoryPackager
    {
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
                                          try {
                                              using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read,
                                                  FileShare.ReadWrite);

                                              var current = JsonSerializer.Deserialize<ExchangeInfo>(stream);
                                              return current;
                                          }
                                          catch {
                                              // We suppress all reading warning here caused by potential pending reads 
                                              // TODO : think of a better way 

                                              return null;
                                          }
                                      }).Where(e => e != null).OfType<ExchangeInfo>(); 

            var connections =
                DirectoryArchiveHelper.EnumerateConnectionFileCandidates(directory)
                                      .Select(fileInfo =>
                                      {
                                          try {
                                              using var stream = fileInfo.Open(FileMode.Open, FileAccess.Read,
                                                  FileShare.ReadWrite);

                                              var current = JsonSerializer.Deserialize<ConnectionInfo>(stream);
                                              return current;
                                          }
                                          catch {
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
            // TODO : to be injected 
            var formatSettings = new FormatSettings(); 

            var directoryArchiveReader = new DirectoryArchiveReader(directory);

            var serializeModel = new HarSerializeModel(directoryArchiveReader, exchangeInfos,
                connectionInfos.ToDictionary(t => t.Id, t => t), formatSettings);

            JsonSerializer.Serialize(outputStream, serializeModel, 
                GlobalArchiveOption.HttpArchiveSerializerOptions);

            return Task.CompletedTask;
        }
    }
}
