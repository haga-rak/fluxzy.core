// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MessagePack;

namespace Fluxzy
{
    public abstract class DirectoryPackager
    {
        private static readonly Regex DirectoryNameExtractionRegex =
            new(@"ex-(?<exchangeId>\d+).mpack", RegexOptions.Compiled);

        public abstract bool ShouldApplyTo(string fileName);

        public abstract Task Pack(string directory, Stream outputStream, HashSet<int>? exchangeIds);

        public static IEnumerable<ConnectionInfo> ReadConnections(IReadOnlyCollection<PackableFile> packableFiles)
        {
            return packableFiles.Where(p => p.Type == PackableFileType.Connection)
                                .Select(packableFile => {
                                    try {
                                        using var stream = packableFile.File.Open(FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite);

                                        return MessagePackSerializer.Deserialize<ConnectionInfo>(
                                            stream,
                                            GlobalArchiveOption.MessagePackSerializerOptions);
                                    }
                                    catch {
                                        // We suppress all reading warning here caused by potential pending reads 
                                        // TODO : think of a better way 

                                        return null;
                                    }
                                })
                                .Where(e => e != null).OfType<ConnectionInfo>();
        }

        public static IEnumerable<ExchangeInfo> ReadExchanges(IReadOnlyCollection<PackableFile> packableFiles)
        {
            return packableFiles.Where(p => p.Type == PackableFileType.Exchange)
                                .Select(packableFile => {
                                    try {
                                        using var stream = packableFile.File.Open(FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite);

                                        var current = MessagePackSerializer.Deserialize<ExchangeInfo>(stream,
                                            GlobalArchiveOption.MessagePackSerializerOptions);

                                        return current;
                                    }
                                    catch {
                                        // We suppress all reading warning here caused by potential pending reads 
                                        // TODO : think of a better way 
                                        return null;
                                    }
                                }).Where(e => e != null).OfType<ExchangeInfo>();
        }

        /// <summary>
        /// </summary>
        /// <param name="fileName">File name (without path)</param>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        internal static bool TryReadExchangeId(string fileName, out int exchangeId)
        {
            // TODO replace regex by a faster string parsing

            if (fileName.StartsWith("ex-") && fileName.EndsWith(".mpack")) {
                var match = DirectoryNameExtractionRegex.Match(fileName);

                if (match.Success) {
                    exchangeId = int.Parse(match.Groups["exchangeId"].Value);

                    return true;
                }
            }

            exchangeId = -1;

            return false;
        }

        /// <summary>
        ///     Read all FileInfo candidates for packaging
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <param name="exchangeIds"></param>
        /// <returns></returns>
        internal static IEnumerable<PackableFile> GetPackableFileInfos(
            DirectoryInfo directoryInfo, HashSet<int>? exchangeIds)
        {
            var exchangeFiles = DirectoryArchiveHelper.EnumerateExchangeFileCandidates(directoryInfo.FullName);
            var baseDirectory = directoryInfo.FullName;

            var connectionIds = new HashSet<int>();

            var metaPath = new FileInfo(DirectoryArchiveHelper.GetMetaPath(baseDirectory));

            yield return new PackableFile(metaPath, PackableFileType.Meta);

            foreach (var file in exchangeFiles) {
                if (!TryReadExchangeId(file.Name, out var exchangeId))
                    continue;

                if (exchangeIds != null && !exchangeIds.Contains(exchangeId))
                    continue;

                using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    // a SAX based option may be a better choice here

                    var exchangeBaseInfo = MessagePackSerializer.Deserialize<ExchangeIdentifiersInfo>(stream,
                        GlobalArchiveOption.MessagePackSerializerOptions);

                    if (exchangeBaseInfo.ConnectionId > 0)
                        connectionIds.Add(exchangeBaseInfo.ConnectionId);
                }

                yield return new PackableFile(file, PackableFileType.Exchange);

                var requestFileInfo =
                    new FileInfo(DirectoryArchiveHelper.GetContentRequestPath(baseDirectory, exchangeId));

                if (requestFileInfo.Exists && requestFileInfo.Length > 0)
                    yield return new PackableFile(requestFileInfo, PackableFileType.RequestBody);

                var responseFileInfo =
                    new FileInfo(DirectoryArchiveHelper.GetContentResponsePath(baseDirectory, exchangeId));

                if (responseFileInfo.Exists && responseFileInfo.Length > 0)
                    yield return new PackableFile(responseFileInfo, PackableFileType.ResponseBody);
            }

            var connectionFiles = DirectoryArchiveHelper.EnumerateConnectionFileCandidates(directoryInfo.FullName);

            foreach (var file in connectionFiles) {
                if (!int.TryParse(file.Name.Replace("con-", string.Empty).Replace(".mpack", string.Empty),
                        out var connectionId))
                    continue;

                if (!connectionIds.Contains(connectionId))
                    continue;

                yield return new PackableFile(file, PackableFileType.Connection);

                var captureFileInfo = new FileInfo(DirectoryArchiveHelper.GetCapturePath(baseDirectory, connectionId));

                if (captureFileInfo.Exists && captureFileInfo.Length > 0)
                    yield return new PackableFile(captureFileInfo, PackableFileType.Capture);

                var captureFileInfoKey =
                    new FileInfo(DirectoryArchiveHelper.GetCapturePathNssKey(baseDirectory, connectionId));

                if (captureFileInfoKey.Exists && captureFileInfoKey.Length > 0)
                    yield return new PackableFile(captureFileInfoKey, PackableFileType.CaptureNssKey);
            }
        }
    }

    public class PackableFile
    {
        public PackableFile(FileInfo file, PackableFileType type)
        {
            File = file;
            Type = type;
        }

        public FileInfo File { get; }

        public PackableFileType Type { get; }
    }

    public enum PackableFileType
    {
        Meta = 100,
        Exchange = 1,
        Connection = 2,
        Capture = 3,
        CaptureNssKey = 4,
        RequestBody = 10,
        ResponseBody = 11
    }
}
