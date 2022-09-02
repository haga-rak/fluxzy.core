// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/file-content")]
    [ApiController]
    public class FileContentController : ControllerBase
    {
        private readonly IObservable<TrunkState> _trunkObservable;

        public FileContentController(IObservable<TrunkState> trunkObservable)
        {
            _trunkObservable = trunkObservable;
        }

        [HttpPost("read")]
        public async Task<ActionResult<TrunkState?>> ReadState()
        {
            return await _trunkObservable.FirstAsync();
        }
    }
}