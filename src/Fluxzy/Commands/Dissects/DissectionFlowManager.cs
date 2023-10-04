using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionFlowManager
    {
        private readonly IEnumerable<IDissectionFormatter> _formatters;
        private readonly List<IDissectionFilter> _dissectionFilters; // to be loaded statically

        public DissectionFlowManager(IEnumerable<IDissectionFilter> dissectionFilters,
            IEnumerable<IDissectionFormatter> formatters)
        {
            _formatters = formatters;
            _dissectionFilters = dissectionFilters.ToList();
        }

        public async Task<bool> Apply(
            IArchiveReader archiveReader,
            Stream stdoutStream, 
            Stream stdErrorStream)
        {
            var exchangeInfos = archiveReader.ReadAllExchanges().ToList();
            var connectionInfos = archiveReader.ReadAllConnections().ToList()
                                               .ToDictionary(t => t.Id, t => t);

            
                


        }
    }

    internal class DissectionOptions
    {
        public bool MustBeUnique { get; set; }
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
