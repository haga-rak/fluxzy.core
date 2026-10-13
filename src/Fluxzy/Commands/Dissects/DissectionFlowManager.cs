// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects.Json;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionFlowManager
    {
        private readonly SequentialFormatter _formatter;
        private readonly Dictionary<string, IDissectionFormatter<EntryInfo>> _formatterMap;
        private readonly IReadOnlyCollection<IDissectionFormatter<EntryInfo>> _formatters;

        public DissectionFlowManager(
            SequentialFormatter formatter,
            IReadOnlyCollection<IDissectionFormatter<EntryInfo>> formatters)
        {
            _formatter = formatter;
            _formatters = formatters;
            _formatterMap = formatters.ToDictionary(t => t.Indicator, t => t, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// </summary>
        /// <param name="archiveReader"></param>
        /// <param name="stdoutStream"></param>
        /// <param name="stdErrorStream"></param>
        /// <param name="dissectionOptions"></param>
        /// <returns></returns>
        public async Task<bool> Apply(
            IArchiveReader archiveReader,
            Stream stdoutStream,
            Stream stdErrorStream, DissectionOptions dissectionOptions)
        {
            var exchangeInfos = archiveReader.ReadAllExchanges().ToList();

            var connectionInfos = archiveReader.ReadAllConnections().ToList()
                                               .ToDictionary(t => t.Id, t => t);

            var filter = dissectionOptions.Filter;
            var matcher = filter.Compile();

            IEnumerable<ExchangeInfo> query = exchangeInfos.Where(matcher.Match);

            if (filter.Offset > 0) {
                query = query.Skip(filter.Offset);
            }

            if (filter.Limit != null) {
                query = query.Take(filter.Limit.Value);
            }

            var filteredExchangeInfos = query.ToList();

            await using var stdErrorWriter = new StreamWriter(stdErrorStream, leaveOpen: true);
            await using var writer = new StreamWriter(stdoutStream, new UTF8Encoding(false), leaveOpen: true);

            if (dissectionOptions.MustBeUnique && filteredExchangeInfos.Count != 1) {
                await stdErrorWriter.WriteLineAsync(
                    $"Error: results not unique ({filteredExchangeInfos.Count}) when --unique option set");

                return false;
            }

            if (dissectionOptions.OutputMode != DissectOutputMode.Format) {
                await WriteJson(archiveReader, filteredExchangeInfos, connectionInfos, writer,
                    dissectionOptions.OutputMode == DissectOutputMode.NdJson, dissectionOptions.WithHeaders);

                return true;
            }

            var first = true;

            foreach (var exchangeInfo in filteredExchangeInfos) {
                if (first) {
                    first = false;
                }
                else {
                    await writer.WriteLineAsync();
                }

                connectionInfos.TryGetValue(exchangeInfo.ConnectionId, out var connectionInfo);

                await _formatter.Format(dissectionOptions.Format, _formatterMap, writer, stdErrorWriter,
                    new EntryInfo(exchangeInfo, connectionInfo, archiveReader, dissectionOptions.MaxBodyBytes));
            }

            return true;
        }

        private static async Task WriteJson(
            IArchiveReader archiveReader,
            List<ExchangeInfo> exchanges,
            Dictionary<int, ConnectionInfo> connectionInfos,
            StreamWriter writer, bool ndJson, bool withHeaders)
        {
            IEnumerable<ExchangeJsonModel> Models()
            {
                foreach (var exchangeInfo in exchanges) {
                    connectionInfos.TryGetValue(exchangeInfo.ConnectionId, out var connectionInfo);

                    yield return ExchangeJsonModel.Create(exchangeInfo, connectionInfo, archiveReader, withHeaders);
                }
            }

            if (ndJson) {
                foreach (var model in Models()) {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(model, DissectJsonOptions.Compact));
                }

                await writer.FlushAsync();

                return;
            }

            await writer.FlushAsync();

            await JsonSerializer.SerializeAsync(writer.BaseStream, Models(), DissectJsonOptions.Indented);

            await writer.BaseStream.FlushAsync();
        }
    }
}
