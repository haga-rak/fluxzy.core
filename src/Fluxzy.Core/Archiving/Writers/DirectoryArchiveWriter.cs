// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Fluxzy.Core;
using Fluxzy.Misc;
using Fluxzy.Rules.Filters;
using MessagePack;

namespace Fluxzy.Writers
{
    public class DirectoryArchiveWriter : RealtimeArchiveWriter
    {
        private readonly ArchiveMetaInformation _archiveMetaInformation = CreateNewCaptureArchiveMetaInformation();

        private static ArchiveMetaInformation CreateNewCaptureArchiveMetaInformation()
        {
            var metaInformation = new ArchiveMetaInformation {
                EnvironmentInformation = new EnvironmentInformation(
                    RuntimeInformation.OSDescription,
#if NET6_0_OR_GREATER
                    RuntimeInformation.RuntimeIdentifier,
#else
                    "unknown",
#endif
                    FluxzySharedSetting.SkipCollectingEnvironmentInformation ? "": Environment.MachineName
                )
            };

            return metaInformation;
        }

        private readonly string _archiveMetaInformationPath;
        private readonly string _baseDirectory;
        private readonly string _captureDirectory;
        private readonly string _contentDirectory;
        private readonly Filter? _saveFilter;
        private readonly string _errorDirectory;
        private readonly string _exchangeDirectory;
        private readonly string _connectionDirectory;

        public DirectoryArchiveWriter(string baseDirectory, Filter? saveFilter)
        {
            _baseDirectory = baseDirectory;
            _saveFilter = saveFilter;
            _contentDirectory = Path.Combine(baseDirectory, "contents");
            _captureDirectory = Path.Combine(baseDirectory, "captures");
            _errorDirectory = Path.Combine(baseDirectory, "errors");
            _exchangeDirectory = Path.Combine(baseDirectory, "exchanges");
            _connectionDirectory = Path.Combine(baseDirectory, "connections");

            _archiveMetaInformationPath = DirectoryArchiveHelper.GetMetaPath(baseDirectory);
        }
        
        public override void Init()
        {
            base.Init();

            _archiveMetaInformation.CaptureDate = DateTime.Now;

            Directory.CreateDirectory(_contentDirectory);
            Directory.CreateDirectory(_captureDirectory);
            Directory.CreateDirectory(_errorDirectory);
            Directory.CreateDirectory(_exchangeDirectory);
            Directory.CreateDirectory(_connectionDirectory);

            UpdateMeta(false);
        }

        private void UpdateMeta(bool force)
        {
            if (!force && File.Exists(_archiveMetaInformationPath))
                return;

            using var fileStream = File.Create(_archiveMetaInformationPath);
            JsonSerializer.Serialize(fileStream, _archiveMetaInformation, GlobalArchiveOption.DefaultSerializerOptions);
        }

        public override void UpdateTags(IEnumerable<Tag> tags)
        {
            foreach (var tag in tags) {
                _archiveMetaInformation.Tags.Add(tag);
            }

            UpdateMeta(true);
        }

        protected override bool ExchangeUpdateRequired(Exchange exchange)
        {
            if (_saveFilter != null && !_saveFilter.Apply(null, exchange.Authority, exchange, null))
                return false;

            return true;
        }

        protected override bool ConnectionUpdateRequired(Connection connection)
        {
            //if (_saveFilter != null && !_saveFilter.Apply(null, connection.Authority, null, null))
            //    return false;

            return true; 
        }

        public override bool Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            var exchangePath = DirectoryArchiveHelper.GetExchangePath(_baseDirectory, exchangeInfo);

            DirectoryArchiveHelper.CreateDirectory(exchangePath);

            using (var fileStream = File.Create(exchangePath)) {
                MessagePackSerializer.Serialize(fileStream, exchangeInfo,
                    GlobalArchiveOption.MessagePackSerializerOptions);

                // JsonSerializer.Serialize(fileStream, exchangeInfo, GlobalArchiveOption.DefaultSerializerOptions);
            }

            if (exchangeInfo.Tags?.Any() ?? false) {
                var modified = false;

                foreach (var tag in exchangeInfo.Tags) {
                    modified = _archiveMetaInformation.Tags.Add(tag) || modified;
                }

                if (modified)
                    UpdateMeta(true);
            }

            return true;
        }

        public override void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(_baseDirectory, connectionInfo);

            DirectoryArchiveHelper.CreateDirectory(connectionPath);

            using var fileStream = File.Create(connectionPath);

            MessagePackSerializer.Serialize(fileStream, connectionInfo,
                GlobalArchiveOption.MessagePackSerializerOptions, cancellationToken);
        }

        protected override void InternalUpdate(DownstreamErrorInfo errorInfo, CancellationToken cancellationToken)
        {
            var errorPath = DirectoryArchiveHelper.GetErrorPath(_baseDirectory);
            
            MessagePackQueueExtensions.AppendMultiple(errorPath, errorInfo,
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public override Stream CreateRequestBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}.data");

            return File.Create(path);
        }

        public override Stream CreateResponseBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}.data");

            return File.Create(path);
        }

        public override Stream CreateWebSocketRequestContent(int exchangeId, int messageId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}-ws-{messageId}.data");

            return File.Open(path, FileMode.Append, FileAccess.Write);
        }

        public override Stream CreateWebSocketResponseContent(int exchangeId, int messageId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}-ws-{messageId}.data");

            return File.Open(path, FileMode.Append, FileAccess.Write);
        }

        public override string GetDumpfilePath(int connectionId)
        {
            return Path.Combine(_captureDirectory, $"{connectionId}.pcapng");
        }

        public override void ClearErrors()
        {
            var errorPath = DirectoryArchiveHelper.GetErrorPath(_baseDirectory);

            ErrorCount = 0;

            if (File.Exists(errorPath))
                File.Delete(errorPath);
        }

        public (int ExchangeId, int ConnectionId) GetNextIds()
        {
            var maxExchangeIds = 
                new DirectoryInfo(DirectoryArchiveHelper.GetExchangeDirectory(_baseDirectory))
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Select(x => {
                        var res = DirectoryArchiveHelper.TryParseIds(x.Name, out var ids);
                        return res ? ids.EndId : 0; 
                    })
                    .OrderByDescending(r => r)
                    .DefaultIfEmpty(0)
                    .First();

            var maxConnectionIds =
                new DirectoryInfo(DirectoryArchiveHelper.GetConnectionDirectory(_baseDirectory))
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Select(x => {
                        var res = DirectoryArchiveHelper.TryParseIds(x.Name, out var ids);
                        return res ? ids.EndId: 0; 
                    })
                    .OrderByDescending(r => r)
                    .DefaultIfEmpty(0)
                    .First();

            // Ids doesn't have to be contiguous, so we add a margin
            return (maxExchangeIds + 1 , maxConnectionIds + 1);
        }

        public void WriteAsset(string relativePath, Stream stream)
        {
            var path = Path.Combine(_baseDirectory, relativePath);
            var fullPath = new FileInfo(path); 

            fullPath.Directory?.Create();

            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }
    }
}
