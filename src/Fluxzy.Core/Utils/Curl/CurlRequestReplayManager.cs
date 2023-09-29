// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Misc;
using Fluxzy.Readers;

namespace Fluxzy.Utils.Curl
{
    public class CurlRequestReplayManager : IRequestReplayManager
    {
        private readonly CurlRequestConverter _requestConverter;
        private readonly IRunningProxyProvider _runningProxyProvider;

        public CurlRequestReplayManager(
            CurlRequestConverter requestConverter, IRunningProxyProvider runningProxyProvider)
        {
            _requestConverter = requestConverter;
            _runningProxyProvider = runningProxyProvider;
        }

        public async Task<bool> Replay(IArchiveReader archiveReader, ExchangeInfo exchangeInfo, bool runInLiveEdit = false)
        {
            var configuration = await _runningProxyProvider.GetConfiguration();

            var curlCommandResult =
                _requestConverter.BuildCurlRequest(archiveReader, exchangeInfo, configuration, runInLiveEdit);

            var args = curlCommandResult.GetProcessCompatibleArgs();

            if (!ProcessUtils.IsCommandAvailable("curl"))
                return false;

            try {
                return await CurlUtility.RunCurl(args, null);
            }
            catch {
                // We just ignore all run errors 

                return false;
            }
        }
    }
}
