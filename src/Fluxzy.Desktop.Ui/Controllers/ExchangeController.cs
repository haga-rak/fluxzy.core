// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients.H11;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Metrics;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Utils;
using Fluxzy.Utils.Curl;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController
    {
        private readonly ProducerFactory _producerFactory;

        public ExchangeController(ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        [HttpGet("{exchangeId}")]
        public async Task<ActionResult<ExchangeInfo>> GetExchange(
            int exchangeId,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get()!;

            var exchange = archiveReader!.ReadExchange(exchangeId);

            if (exchange == null)
                return new NotFoundResult();

            return exchange;
        }

        [HttpGet("{exchangeId}/has-request-body")]
        public async Task<ActionResult<bool>> HasRequestBody(
            int exchangeId,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get()!;

            return archiveReader!.HasRequestBody(exchangeId);
        }

        [HttpGet("{exchangeId}/has-response-body")]
        public async Task<ActionResult<bool>> HasResponseBody(
            int exchangeId,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get();

            return archiveReader!.HasResponseBody(exchangeId);
        }

        [HttpGet("{exchangeId}/suggested-request-body-file-name")]
        public async Task<ActionResult<string>> GetSuggestedRequestBodyFileName(
            int exchangeId,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get();
            var exchange = archiveReader!.ReadExchange(exchangeId)!;

            return new JsonResult(ExchangeUtility.GetRequestBodyFileNameSuggestion(exchange));
        }

        [HttpGet("{exchangeId}/suggested-response-body-file-name")]
        public async Task<ActionResult<string>> GetSuggestedResponseBodyFileName(
            int exchangeId,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get();
            var exchange = archiveReader!.ReadExchange(exchangeId)!;

            return new JsonResult(ExchangeUtility.GetResponseBodyFileNameSuggestion(exchange));
        }

        [HttpPost("{exchangeId}/save-request-body")]
        public async Task<ActionResult<bool>> SaveRequestBody(
            int exchangeId,
            [FromBody]
            SaveFileViewModel body,
            [FromServices]
            SaveRequestBodyProducerAction action)
        {
            return await action.Do(exchangeId, body.FileName);
        }

        [HttpPost("{exchangeId}/save-multipart-Content")]
        public async Task<ActionResult<bool>> SaveMultipartContent(
            int exchangeId,
            [FromBody]
            SaveFileMultipartActionModel body,
            [FromServices]
            SaveFileMultipartAction action)
        {
            return await action.Do(exchangeId, body);
        }

        [HttpPost("{exchangeId}/save-response-body")]
        public async Task<ActionResult<bool>> SaveResponseBody(
            [FromServices] SaveResponseBodyAction action,
            int exchangeId, [FromBody] SaveFileViewModel body,
            [FromQuery(Name = "decode")]
            bool decode = true)
        {
            return await action.Do(exchangeId, decode, body.FileName);
        }

        [HttpPost("{exchangeId}/save-ws-body/{direction}/{messageId}")]
        public async Task<ActionResult<bool>> SaveWsBody(
            [FromServices] SaveWebSocketBodyAction action,
            [FromBody]
            SaveFileViewModel body,
            int exchangeId, WsMessageDirection direction, int mesageId)
        {
            return await action.Do(exchangeId, mesageId, direction, body.FileName);
        }

        [HttpGet("{exchangeId}/curl")]
        public async Task<ActionResult<CurlCommandResult>> GetCurlCommand(
            [FromServices] CurlRequestConverter converter,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider,
            [FromServices]
            IObservable<ProxyState> proxyStateProvider,
            [FromServices]
            IRunningProxyProvider runningProxyProvider,
            int exchangeId)
        {
            var archiveReader = (await archiveReaderProvider.Get())!;
            var exchangeInfo = archiveReader.ReadExchange(exchangeId);

            if (exchangeInfo == null)
                return new NotFoundObjectResult(exchangeId);

            var config = await runningProxyProvider.GetConfiguration();
            var request = converter.BuildCurlRequest(archiveReader, exchangeInfo, config);

            return request;
        }

        [HttpPost("{exchangeId}/save-curl-payload/{fileId}")]
        public async Task<ActionResult<bool>> SaveCurlPayload(
            int exchangeId,
            Guid fileId,
            [FromBody]
            SaveFileViewModel body,
            [FromServices]
            CurlExportFolderManagement curlExportFolderManagement)
        {
            return await curlExportFolderManagement.SaveTo(fileId, body.FileName);
        }

        [HttpPost("{exchangeId}/replay")]
        public async Task<ActionResult<bool>> Replay(
            int exchangeId,
            [FromServices]
            IRequestReplayManager requestReplayManager,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider,
            [FromQuery(Name = "runInLiveEdit")]
            bool runInLiveEdit = false)
        {
            var archiveReader = await archiveReaderProvider.Get();
            var exchangeInfo = archiveReader!.ReadExchange(exchangeId);

            if (exchangeInfo == null)
                return new NotFoundObjectResult(exchangeId);

            return await requestReplayManager.Replay(archiveReader, exchangeInfo, runInLiveEdit);
        }

        [HttpGet("{exchangeId}/metrics")]
        public async Task<ActionResult<ExchangeMetricInfo>> GetMetrics(
            int exchangeId,
            [FromServices]
            ExchangeMetricBuilder exchangeMetricBuilder,
            [FromServices]
            IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get();
            var metric = exchangeMetricBuilder.Get(exchangeId, archiveReader!);

            if (metric == null)
                return new NotFoundObjectResult(exchangeId);

            return metric;
        }

        public record SaveFileViewModel(string FileName);
    }
}
