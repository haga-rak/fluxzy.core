using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionFlowManager
    {
        public DissectionFlowManager()
        {

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

            var filteredExchangeInfos = exchangeInfos.Where(exchangeInfo =>
                dissectionOptions.Filters
                                 .Any(filter => filter.IsApplicable(exchangeInfo, connectionInfos[exchangeInfo.ConnectionId])));



            foreach (var exchangeInfo in filteredExchangeInfos) {

            }
                


        }
    }

    internal interface IDissectionFilter
    {
        bool IsApplicable(ExchangeInfo exchangeInfo, ConnectionInfo connectionInfo);
    }

    internal interface IDissectionFormatter
    {
        string Indicator { get; }

        Task Apply(ExchangeInfo exchangeInfo, Stream stdoutStream);
    }
}
