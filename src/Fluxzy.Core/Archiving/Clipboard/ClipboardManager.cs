// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Writers;

namespace Fluxzy.Clipboard
{
    public class ClipboardManager
    {
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

        public async Task Paste(CopyPayload payload, RealtimeArchiveWriter archiveWriter)
        {
            // determine max exchange id and connection id 

            var minExchangeId = payload.Exchanges.Min(e => e.Id);
            var minConnectionId = payload.Connections.Min(c => c.Id);


            foreach (var exchangeData in payload.Exchanges) {

                foreach (var artefact in exchangeData.Artefacts) {
                    if (artefact.Binary != null) {
                        await archiveWriter.WriteExchangeArtefact(exchangeData.Id, artefact.Path, artefact.Binary);
                    }
                    else {
                        await archiveWriter.WriteExchangeArtefact(exchangeData.Id, artefact.Path, artefact.FilePath);
                    }
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
