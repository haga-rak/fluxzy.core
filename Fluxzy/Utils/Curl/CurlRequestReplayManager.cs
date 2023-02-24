// Copyright © 2023 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Utils.Curl
{
    public class CurlRequestReplayManager : IRequestReplayManager
    {
        private readonly CurlRequestConverter _requestConverter;
        private readonly IRunningProxyProvider _runningProxyProvider;

        public CurlRequestReplayManager(CurlRequestConverter requestConverter, IRunningProxyProvider runningProxyProvider)
        {
            _requestConverter = requestConverter;
            _runningProxyProvider = runningProxyProvider;
        }

        public async Task<bool> Replay(IArchiveReader archiveReader, ExchangeInfo exchangeInfo)
        {
            var configuration = await _runningProxyProvider.GetConfiguration();
            var curlCommandResult = 
                _requestConverter.BuildCurlRequest(archiveReader, exchangeInfo, configuration);

            var args = curlCommandResult.GetProcessCompatibleArgs();

            if (!CurlUtility.IsCurlInstalled())
                return false;

            try
            {
                return await CurlUtility.RunCurl(args, null);
            }
            catch
            {
                // We just ignore all run errors 
                
                return false; 
            }
        }
    }
}
