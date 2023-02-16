// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Clients.H11;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Utils.Curl;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController
    {
        public record SaveFileViewModel(string FileName); 

        private readonly ProducerFactory _producerFactory;

        public ExchangeController(
            ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        [HttpPost("{exchangeId}/save-request-body")]
        public async Task<ActionResult<bool>> SaveRequestBody(
            int exchangeId,
            [FromBody] SaveFileViewModel body,
            [FromServices] SaveRequestBodyProducerAction action)
        {
            return await action.Do(exchangeId, body.FileName);
        }

        [HttpPost("{exchangeId}/save-multipart-Content")]
        public async Task<ActionResult<bool>> SaveMultipartContent(
            int exchangeId,
            [FromBody] SaveFileMultipartActionModel body,
            [FromServices] SaveFileMultipartAction action)
        {
            return await action.Do(exchangeId, body);
        }


        [HttpPost("{exchangeId}/save-response-body")]
        public async Task<ActionResult<bool>> SaveResponseBody(
            [FromServices] SaveResponseBodyAction action,
            int exchangeId, [FromBody] SaveFileViewModel body,
            [FromQuery(Name = "decode")] bool decode = true)
        {
            return await action.Do(exchangeId,decode, body.FileName);
        }

        [HttpPost("{exchangeId}/save-ws-body/{direction}/{messageId}")]
        public async Task<ActionResult<bool>> SaveWsBody(
            [FromServices] SaveWebSocketBodyAction action,
            [FromBody] SaveFileViewModel body,
            int exchangeId, WsMessageDirection direction, int mesageId)
        {
            return await action.Do(exchangeId, mesageId, direction, body.FileName);
        }

        [HttpGet("{exchangeId}/curl")]
        public async Task<ActionResult<CurlCommandResult>> GetCurlCommand(
            [FromServices] CurlRequestConverter converter,
            [FromServices] IArchiveReaderProvider archiveReaderProvider,
            [FromServices] IObservable<ProxyState> proxyStateProvider,
            int exchangeId)
        {
            var archiveReader = (await archiveReaderProvider.Get())!;
            var exchangeInfo = archiveReader.ReadExchange(exchangeId);

            if (exchangeInfo == null)
                return new NotFoundObjectResult(exchangeId);

            var proxyState = await proxyStateProvider.FirstAsync();

            var request = converter.BuildCurlRequest(archiveReader, exchangeInfo, new CurlProxyConfiguration(
                "127.0.0.1", proxyState.BoundConnections.First().Port));

            return request; 
        }


        [HttpPost("{exchangeId}/save-curl-payload/{fileId}")]
        public async Task<ActionResult<bool>> SaveCurlPayload(
            int exchangeId, 
            Guid fileId,
            [FromBody] SaveFileViewModel body,
            [FromServices] CurlExportFolderManagement curlExportFolderManagement)
        {
            return await curlExportFolderManagement.SaveTo(fileId, body.FileName);
        }
    }
}