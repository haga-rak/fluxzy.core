// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Ui;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Readers;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using static Fluxzy.Desktop.Ui.Controllers.ExchangeController;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController
    {
        private readonly IObservable<IArchiveReader> _archiveReaderObservable;

        public ConnectionController(IObservable<IArchiveReader> archiveReaderObservable)
        {
            _archiveReaderObservable = archiveReaderObservable;
        }

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ConnectionInfo?>> Get(int connectionId)
        {
            var archiveReader = await _archiveReaderObservable.FirstAsync();
            return archiveReader.ReadConnection(connectionId);
        }

        [HttpGet("{connectionId}/capture/check")]
        public async Task<ActionResult<bool>> HasDump(int connectionId, [FromServices] IArchiveReaderProvider archiveReaderProvider)
        {
            var archiveReader = await archiveReaderProvider.Get();
            return archiveReader!.HasCapture(connectionId); 
        }

        [HttpPost("{connectionId}/capture/save")]
        public async Task<ActionResult<bool>> SaveCapture(int connectionId,
            [FromServices] SaveRawCaptureAction saveRawCaptureAction, [FromBody] SaveFileViewModel body, 
            [FromServices] ProxyControl proxyControl)
        {
            proxyControl.TryFlush();
            return await saveRawCaptureAction.Do(connectionId, body.FileName);
        }
        
        [HttpPost("{connectionId}/capture/open")]
        public async Task<ActionResult<bool>> OpenCapture(int connectionId,
            [FromServices] FileExecutionManager fileExecutionManager,
            [FromServices] ProxyControl proxyControl)
        {
            var archiveReader = await _archiveReaderObservable.FirstAsync();
            proxyControl.TryFlush();
            return await fileExecutionManager.OpenPcap(connectionId, archiveReader); 
        }

        [HttpPost("{connectionId}/capture/key")]
        public async Task<ActionResult<string?>> GetCaptureKey(int connectionId,
            [FromServices] FileExecutionManager fileExecutionManager,
            [FromServices] ProxyControl proxyControl)
        {
            var archiveReader = await _archiveReaderObservable.FirstAsync();
            proxyControl.TryFlush();

            return new JsonResult(await fileExecutionManager.GetNssKey(connectionId, archiveReader)); 
        }
    }
}