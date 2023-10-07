using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionFlowManager
    {
        private readonly SequentialFormatter _formatter;
        private readonly IReadOnlyCollection<IDissectionFormatter<EntryInfo>> _formatters;
        private readonly Dictionary<string, IDissectionFormatter<EntryInfo>> _formatterMap;

        public DissectionFlowManager(SequentialFormatter formatter, 
            IReadOnlyCollection<IDissectionFormatter<EntryInfo>> formatters)
        {
            _formatter = formatter;
            _formatters = formatters; 
            _formatterMap = formatters.ToDictionary(t => t.Indicator, t => t, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
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

            var filteredExchangeInfos = exchangeInfos; 

            if (dissectionOptions.ExchangeIds != null && dissectionOptions.ExchangeIds.Any())
                filteredExchangeInfos = filteredExchangeInfos
                    .Where(t => dissectionOptions.ExchangeIds.Contains(t.Id)).ToList();

            await using var stdErrorWriter = new StreamWriter(stdErrorStream, leaveOpen: true);
            await using var writer = new StreamWriter(stdoutStream, new UTF8Encoding(false), leaveOpen: true);

            if (dissectionOptions.MustBeUnique && filteredExchangeInfos.Count != 1) {
                await stdErrorWriter.WriteLineAsync($"Error: results not unique ({filteredExchangeInfos.Count}) when --unique option set");

                return false; 
            }

            foreach (var exchangeInfo in filteredExchangeInfos) {
                connectionInfos.TryGetValue(exchangeInfo.ConnectionId, out var connectionInfo);
                await _formatter.Format(dissectionOptions.Format, _formatterMap, writer, stdErrorWriter,
                    new (exchangeInfo, connectionInfo, archiveReader));

                if (!dissectionOptions.MustBeUnique) {
                    await writer.WriteLineAsync();
                }
            }

            return true;
        }

    }
}
