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
        private readonly IReadOnlyCollection<IDissectionFormatter<ExchangeInfo>> _formatters;
        private readonly Dictionary<string, IDissectionFormatter<ExchangeInfo>> _formatterMap;

        public DissectionFlowManager(SequentialFormatter formatter, 
            IReadOnlyCollection<IDissectionFormatter<ExchangeInfo>> formatters)
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

            var filteredExchangeInfos = (IEnumerable<ExchangeInfo>) exchangeInfos; 

            if (dissectionOptions.ExchangeIds != null)
                filteredExchangeInfos = filteredExchangeInfos.Where(t => dissectionOptions.ExchangeIds.Contains(t.Id));

            using var stdErrorWriter = new StreamWriter(stdErrorStream, leaveOpen: true);
            using var writer = new StreamWriter(stdoutStream, new UTF8Encoding(false), leaveOpen: true);

            foreach (var exchangeInfo in filteredExchangeInfos) {
                await _formatter.Format(dissectionOptions.Format, _formatterMap, writer, stdErrorWriter, exchangeInfo);
            }

            return true;
        }

    }

    internal interface IDissectionFormatter<in T>
    {
        string Indicator { get; }

        Task Write(T exchangeInfo, StreamWriter stdOutWriter);
    }
}
