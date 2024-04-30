// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Writers;
using MessagePack;

namespace Fluxzy.Clipboard
{
    public class ClipboardManager
    {
        private readonly int _idShiftValue;

        public ClipboardManager(int idShiftValue = 100)
        {
            _idShiftValue = idShiftValue;
        }

        public Task<CopyPayload> Copy(IEnumerable<int> exchangeIds, IArchiveReader archiveReader,
            CopyPolicyEnforcer copyPolicyEnforcer)
        {
            var exchangeDatas = new List<ExchangeData>();
            var connectionDatas = new List<ConnectionData>();

            var connectionIds = new HashSet<int>(); 

            foreach (var exchangeId in exchangeIds) {
                var exchange = archiveReader.ReadExchange(exchangeId);

                if (exchange == null)
                    continue;

                connectionIds.Add(exchange.ConnectionId); 

                var artefacts = new List<CopyArtefact>();

                foreach (var exchangeAsset in archiveReader.GetAssetsByExchange(exchangeId)) {
                    var arteFact = copyPolicyEnforcer.Get(exchangeAsset);

                    if (arteFact == null) {
                        continue;
                    }

                    artefacts.Add(arteFact);
                }

                exchangeDatas.Add(new ExchangeData(exchangeId, artefacts));

                // copy exchange artefacts
            }

            foreach (var connectionId in connectionIds) {
                var connection = archiveReader.ReadConnection(connectionId);

                if (connection == null)
                    continue;

                var artefacts = new List<CopyArtefact>();

                foreach (var connectionAsset in archiveReader.GetAssetsByConnection(connectionId)) {
                    var arteFact = copyPolicyEnforcer.Get(connectionAsset);

                    if (arteFact == null) {
                        continue;
                    }

                    artefacts.Add(arteFact);
                }

                connectionDatas.Add(new ConnectionData(connectionId, artefacts));

                // copy connection artefacts
            }

            return Task.FromResult(new CopyPayload(exchangeDatas, connectionDatas));
        }

        public async Task Paste(CopyPayload payload, DirectoryArchiveWriter archiveWriter)
        {
            // determine max exchange id and connection id 

            var nextIds = archiveWriter.GetNextIds(); 

            var exchangeIdShift = nextIds.ExchangeId  + _idShiftValue;
            var connectionIdShift = nextIds.ConnectionId + _idShiftValue;

            foreach (var exchangeData in payload.Exchanges) {

                var exchangeDataRawBinary = exchangeData.Artefacts.FirstOrDefault(a => a.Path.EndsWith(".mpack"));

                if (exchangeDataRawBinary == null) {
                    continue;
                }

                // We had to deserialize to change the exchange id
                var exchangeInfo = MessagePackSerializer.Deserialize<ExchangeInfo>(exchangeDataRawBinary.Binary,
                    GlobalArchiveOption.MessagePackSerializerOptions);

                var oldExchangeId = exchangeInfo.Id;

                exchangeInfo.Id += exchangeIdShift;
                exchangeInfo.ConnectionId += connectionIdShift;

                archiveWriter.Update(exchangeInfo, CancellationToken.None); 

                foreach (var artefact in exchangeData.Artefacts.Where(a => !a.Path.EndsWith(".mpack"))) {

                    if (artefact.Binary == null && artefact.FilePath == null) {
                        continue;
                    }

                    var relativePath = artefact.Path;

                    await using var stream = artefact.Binary != null ? (Stream) new MemoryStream(artefact.Binary) 
                        : File.OpenRead(artefact.FilePath!);

                    // This may be faster than parsing but still not optimal
                    relativePath = relativePath.Replace($"{oldExchangeId}", $"{exchangeInfo.Id}");
                    archiveWriter.WriteAsset(relativePath, stream);
                }
            }

            foreach (var connectionData in payload.Connections) {

                var connectionDataBinary = connectionData.Artefacts.FirstOrDefault(a => a.Path.EndsWith(".mpack"));

                if (connectionDataBinary == null) {
                    continue;
                }

                // We had to deserialize to change the exchange id
                var connectionInfo = MessagePackSerializer.Deserialize<ConnectionInfo>(connectionDataBinary.Binary,
                    GlobalArchiveOption.MessagePackSerializerOptions);

                var oldConnectionId = connectionInfo.Id;

                connectionInfo.Id += connectionIdShift;

                archiveWriter.Update(connectionInfo, CancellationToken.None); 

                foreach (var artefact in connectionData.Artefacts.Where(a => !a.Path.EndsWith(".mpack"))) {

                    if (artefact.Binary == null && artefact.FilePath == null) {
                        continue;
                    }

                    var relativePath = artefact.Path;

                    await using var stream = artefact.Binary != null ? (Stream) new MemoryStream(artefact.Binary) 
                        : File.OpenRead(artefact.FilePath!);

                    // This may be faster than parsing but still not optimal
                    relativePath = relativePath.Replace($"{oldConnectionId}", $"{connectionInfo.Id}");
                    archiveWriter.WriteAsset(relativePath, stream);
                }
            }
        }
    }

    public class CopyArtefact
    {
        public CopyArtefact(string path, string extension, byte[]? binary, string? filePath)
        {
            Path = path;
            Extension = extension;
            Binary = binary;
            FilePath = filePath;
        }

        public string Path { get; }

        public string Extension { get; }

        public byte[]? Binary { get; }

        public string? FilePath { get; }
    }

    public class CopyableData
    {
        public CopyableData(int id, List<CopyArtefact> artefacts)
        {
            Id = id;
            Artefacts = artefacts;
        }

        public int Id { get; }

        public List<CopyArtefact> Artefacts { get; }
    }

    public class ExchangeData : CopyableData
    {
        public ExchangeData(int id, List<CopyArtefact> artefacts)
            : base(id, artefacts)
        {
        }
    }

    public class ConnectionData : CopyableData
    {
        public ConnectionData(int id, List<CopyArtefact> artefacts)
            : base(id, artefacts)
        {
        }
    }

    public class CopyPayload
    {
        public CopyPayload(List<ExchangeData> exchanges, List<ConnectionData> connections)
        {
            Exchanges = exchanges;
            Connections = connections;
        }

        public List<ExchangeData> Exchanges { get; }

        public List<ConnectionData> Connections { get; }
    }

    public class CopyPolicy
    {
        public CopyPolicy(CopyOptionType type, long? maxSize, List<string>? disallowedExtensions)
        {
            Type = type;
            MaxSize = maxSize;
            DisallowedExtensions = disallowedExtensions;
        }

        public CopyOptionType Type { get; }

        public long ? MaxSize { get;  }

        public List<string>? DisallowedExtensions { get; }
    }

    public enum CopyOptionType
    {
        Memory,
        Reference
    }
}
