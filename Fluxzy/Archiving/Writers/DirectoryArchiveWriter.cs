// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Writers
{
    public class DirectoryArchiveWriter : RealtimeArchiveWriter
    {
        private readonly ArchiveMetaInformation _archiveMetaInformation = new();
        private readonly string _archiveMetaInformationPath;
        private readonly string _baseDirectory;
        private readonly string _captureDirectory;
        private readonly string _contentDirectory;
        private readonly Filter? _saveFilter;

        public DirectoryArchiveWriter(string baseDirectory, Filter? saveFilter)
        {
            _baseDirectory = baseDirectory;
            _saveFilter = saveFilter;
            _contentDirectory = Path.Combine(baseDirectory, "contents");
            _captureDirectory = Path.Combine(baseDirectory, "captures");
            _archiveMetaInformationPath = DirectoryArchiveHelper.GetMetaPath(baseDirectory);
        }

        public override void Init()
        {
            base.Init();

            _archiveMetaInformation.CaptureDate = DateTime.Now;

            Directory.CreateDirectory(_contentDirectory);
            Directory.CreateDirectory(_captureDirectory);

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

        public override bool Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            if (_saveFilter != null && !_saveFilter.Apply(null, exchangeInfo, null))
                return false;

            var exchangePath = DirectoryArchiveHelper.GetExchangePath(_baseDirectory, exchangeInfo);

            DirectoryArchiveHelper.CreateDirectory(exchangePath);

            using (var fileStream = File.Create(exchangePath)) {
                JsonSerializer.Serialize(fileStream, exchangeInfo, GlobalArchiveOption.DefaultSerializerOptions);
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
            JsonSerializer.Serialize(fileStream, connectionInfo, GlobalArchiveOption.DefaultSerializerOptions);
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
    }
}
