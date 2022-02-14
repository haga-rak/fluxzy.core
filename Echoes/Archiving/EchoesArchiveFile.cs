using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Echoes.Core.Utils;

namespace Echoes
{
    /// <summary>
    /// Read Write archive file 
    /// </summary>
    ///
    //public class EchoesArchiveFile : IDisposable
    //{
    //    private readonly Task _updateTask; 
    //    private readonly BufferBlock<object> _todos = new BufferBlock<object>();
    //    private readonly CancellationTokenSource _updateCancellationTokenSource = new CancellationTokenSource();
    //    private readonly SemaphoreSlim _fileLocker = new SemaphoreSlim(1,1);

    //    private EchoesArchiveFile(string fileName, bool createNew)
    //    {
    //        FileName = fileName;

    //        if (!createNew && !File.Exists(fileName))
    //        {
    //            throw new FileNotFoundException("Zip file was not found", fileName);
    //        }

    //        if (createNew)
    //        {
    //            File.Delete(fileName);
    //            // Create an empty zip file 
    //            new ZipArchive(File.OpenWrite(fileName), ZipArchiveMode.Create).Dispose();
    //        }
    //        else
    //        {
    //            new ZipArchive(File.OpenRead(fileName), ZipArchiveMode.Read).Dispose();
    //            // Ensure current file is a valid zip file
    //        }

    //        _updateTask = Task.Run(WriteProcess);
    //    }

    //    public string FileName { get; }

    //    private async Task WriteProcess()
    //    {
    //        try
    //        {
    //            while (await _todos.OutputAvailableAsync().ConfigureAwait(false))
    //            {
    //                if (_todos.TryReceiveAll(out var items))
    //                {
    //                    foreach (var grouppedItem in items.GroupAdjacent(t => t is HttpExchange[]))
    //                    {
    //                        if (grouppedItem.Key)
    //                        {
    //                            var exchanges = grouppedItem.OfType<HttpExchange[]>().SelectMany(s => s).ToArray();
    //                            await InternalAppend(exchanges).ConfigureAwait(false);
    //                        }
    //                        else
    //                        {
    //                            foreach (var messageId in grouppedItem.OfType<Guid>())
    //                            {
    //                                await InternalRemove(messageId).ConfigureAwait(false);
    //                            }
    //                        }
    //                    }
                        
    //                }

    //            }
    //        }
    //        catch (InvalidOperationException)
    //        {
    //            // Probably source complete, natural death 
    //        }
    //    }

    //    private async Task InternalAppend(params HttpExchange [] httpExchanges)
    //    {
    //        // should be thread safe 
            
    //        using (await QuickSlim.Lock(_fileLocker).ConfigureAwait(false))
    //        using (var zipArchive = ZipFile.Open(FileName, ZipArchiveMode.Update))
    //        {
    //            foreach (var httpExchange in httpExchanges)
    //            {
    //                var mainEntry =
    //                    zipArchive.CreateEntry(EchoesArchivePathHelper.GetMessageEntryName(httpExchange));

    //                using (var stream = mainEntry.Open())
    //                using (var streamWriter = new StreamWriter(stream))
    //                {
    //                    var content = httpExchange.ToSerializedString();
    //                    await streamWriter.WriteAsync(content).ConfigureAwait(false);
    //                }

    //                // Adding content 

    //                if (!httpExchange.RequestMessage.NoBody)
    //                {
    //                    using (var requestBodyStream = httpExchange.RequestMessage.ReadBodyAsStream())
    //                    {
    //                        var requestEntry = zipArchive.CreateEntry(EchoesArchivePathHelper.GetMessageContentEntryName(httpExchange.RequestMessage));
    //                        using (var stream = requestEntry.Open())
    //                        {
    //                            await requestBodyStream.CopyToAsync(stream).ConfigureAwait(false);
    //                        }
    //                    }
    //                }

    //                httpExchange.RequestMessage.ArchiveReference = this;

    //                var responseMessage = httpExchange.ResponseMessage;

    //                if (responseMessage == null)
    //                    continue;
                    
    //                if (responseMessage.NoBody)
    //                    continue;

    //                using (var responseBodyStream = responseMessage.ReadBodyAsStream())
    //                {
    //                    var responseEntry = zipArchive.CreateEntry(EchoesArchivePathHelper.GetMessageContentEntryName(responseMessage));

    //                    using (var stream = responseEntry.Open())
    //                    {
    //                        await responseBodyStream.CopyToAsync(stream).ConfigureAwait(false);
    //                    }
    //                }

    //                responseMessage.ArchiveReference = this;
                    
    //            }
    //        }
    //    }

    //    private async Task<bool> InternalRemove(Guid messageId)
    //    {
    //        var entriesFound = false; 

    //        using (await QuickSlim.Lock(_fileLocker).ConfigureAwait(false))
    //        using (var zipArchive = ZipFile.Open(FileName, ZipArchiveMode.Update))
    //        {
    //            var entriesToBeRemoved = zipArchive.Entries.Where(e => e.FullName.StartsWith($"data/{messageId}")).ToList();

    //            foreach (var entry in entriesToBeRemoved)
    //            {
    //                entry.Delete();
    //                entriesFound = true; 
    //            }
    //        }

    //        return entriesFound; 
    //    }

    //    public async Task Append(params HttpExchange[] httpExchanges)
    //    {
    //        await _todos.SendAsync(httpExchanges).ConfigureAwait(false);
    //    }

    //    public async Task Remove(Guid messageId)
    //    {
    //        await _todos.SendAsync(messageId).ConfigureAwait(false);
    //    }

    //    public async Task AppendAndWaitForComplete(params HttpExchange[] httpExchanges)
    //    {
    //        await InternalAppend(httpExchanges).ConfigureAwait(false);
    //    }

    //    public async Task WaitForNoPendingTask()
    //    {
    //        await AppendAndWaitForComplete().ConfigureAwait(false);
    //    }

    //    public async Task<bool> RemoveAndWaitForComplete(Guid messageId)
    //    {
    //        return await InternalRemove(messageId).ConfigureAwait(false);
    //    }

    //    internal Stream InternalReadContentBody(HttpMessage message)
    //    {
    //        using (var zipArchive = ZipFile.Open(FileName, ZipArchiveMode.Read))
    //        {
    //            var entryName = EchoesArchivePathHelper.GetMessageContentEntryName(message);
    //            var entry =
    //                zipArchive.Entries.FirstOrDefault(e => e.FullName == entryName); 
                
    //            if (entry == null)
    //                return new MemoryStream(new byte[0]);

    //            using (var entryStream = entry.Open())
    //            {
    //                var memoryStreamResult = new MemoryStream();
    //                entryStream.CopyTo(memoryStreamResult);
    //                memoryStreamResult.Seek(0, SeekOrigin.Begin);

    //                return memoryStreamResult;
    //            }
    //        }
    //    }

    //    public async Task<Stream> ReadContentBody(HttpMessage message)
    //    {
    //        using (await QuickSlim.Lock(_fileLocker).ConfigureAwait(false))
    //        {
    //            return InternalReadContentBody(message);
    //        }
    //    }
        
    //    public async Task<EchoesArchive> ReadArchives()
    //    {
    //        using (await QuickSlim.Lock(_fileLocker).ConfigureAwait(false))
    //        using (var zipArchive = ZipFile.Open(FileName, ZipArchiveMode.Read))
    //        {
    //            var entries = zipArchive.Entries.Where(e => e.FullName.EndsWith("/def.json"));

    //            var exchanges = entries.Select(zipArchiveEntry =>
    //            {
    //                using (var streamReader = new StreamReader(zipArchiveEntry.Open()))
    //                {
    //                    var result = HttpMessageSerializationExtensions.FromSerializedString(streamReader.ReadToEnd());

    //                    result.RequestMessage.ArchiveReference = this;

    //                    if (result.ResponseMessage != null)
    //                        result.ResponseMessage.ArchiveReference = this; 

    //                    return result; 
    //                }

    //            }).ToList();

    //            return new EchoesArchive(exchanges);
    //        }
    //    }

    //    /// <summary>
    //    /// Duplicate current archive file to destination
    //    /// </summary>
    //    /// <param name="fileName"></param>
    //    /// <returns></returns>
    //    public async Task CopyTo(string fileName)
    //    {
    //        using (await QuickSlim.Lock(_fileLocker).ConfigureAwait(false))
    //        {
    //            await FileExtension.CopyAsync(FileName, fileName).ConfigureAwait(false);
    //        }
    //    }

    //    public static async Task<EchoesArchiveFile> Create(string fileName, params HttpExchange [] initialDatas)
    //    {
    //        var result = new EchoesArchiveFile(fileName, true);
    //        await result.AppendAndWaitForComplete(initialDatas).ConfigureAwait(false);
    //        return result; 
    //    }

    //    public static EchoesArchiveFile Create(string fileName)
    //    {
    //        var result = new EchoesArchiveFile(fileName, true);
    //        return result; 
    //    }
        
    //    public static EchoesArchiveFile OpenRead(string fileName)
    //    {
    //        return new EchoesArchiveFile(fileName, false);
    //    }


    //    public static async Task<EchoesArchiveFile> OpenClone(string sourceFileName, string cloneFileName)
    //    {
    //        if (!File.Exists(sourceFileName))
    //            throw new FileNotFoundException($"{sourceFileName} does not exist");

    //        await FileExtension.CopyAsync(sourceFileName, cloneFileName).ConfigureAwait(false);

    //        return OpenRead(cloneFileName); 
    //    }


    //    public void Dispose()
    //    {
    //        _todos.Complete(); // Halt the write process

    //        _updateCancellationTokenSource.Cancel();
    //        _updateTask.ConfigureAwait(false).GetAwaiter().GetResult();
    //        _updateCancellationTokenSource.Dispose();

    //        _fileLocker.Dispose();
    //    }
    //}

    internal class FileExtension
    {
        public static async Task CopyAsync(string sourceFileName, string destinationFileName)
        {
            await using var sourceStream = File.OpenRead(sourceFileName);
            await using var destinationStream = File.Create(destinationFileName);

            await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
        }
    }
}