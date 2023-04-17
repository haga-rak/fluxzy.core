// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/file-content")]
    [ApiController]
    public class FileContentController : ControllerBase
    {
        private readonly IObservable<FileContentOperationManager> _fileContentOperationManager;
        private readonly IObservable<TrunkState> _trunkObservable;

        public FileContentController(
            IObservable<TrunkState> trunkObservable, IObservable<FileContentOperationManager>
                fileContentOperationManager)
        {
            _trunkObservable = trunkObservable;
            _fileContentOperationManager = fileContentOperationManager;
        }

        [HttpPost("read")]
        public async Task<ActionResult<TrunkState?>> ReadState(
            [FromServices] IObservable<FilteredExchangeState?> filterExchangeStateObservable)
        {
            var trunkState = await _trunkObservable.FirstAsync();
            var filteredExchangeState = await filterExchangeStateObservable.FirstAsync();

            if (filteredExchangeState == null)
                return trunkState;

            return trunkState.ApplyFilter(filteredExchangeState);
        }

        [HttpPost("delete")]
        public async Task<ActionResult<TrunkState>> Delete(
            FileContentDelete deleteOp,
            [FromServices]
            IObservable<FilteredExchangeState?> filterExchangeStateObservable)
        {
            (await _fileContentOperationManager.FirstAsync()).Delete(deleteOp);

            var trunkState = await _trunkObservable.FirstAsync();
            var filteredExchangeState = await filterExchangeStateObservable.FirstAsync();

            if (filteredExchangeState == null)
                return trunkState;

            return trunkState.ApplyFilter(filteredExchangeState);
        }

        [HttpDelete("")]
        public async Task<ActionResult<TrunkState>> Clear(
            [FromServices] IObservable<FilteredExchangeState?> filterExchangeStateObservable)
        {
            (await _fileContentOperationManager.FirstAsync()).Clear();

            return await _trunkObservable.FirstAsync();
        }
    }
}
