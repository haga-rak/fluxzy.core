// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrunkController : ControllerBase
    {
        private readonly TrunkManager _trunkManager;
        private readonly FileManager _fileManager;

        public TrunkController(TrunkManager trunkManager, FileManager fileManager)
        {
            _trunkManager = trunkManager;
            _fileManager = fileManager;
        }

        [HttpPost("read")]
        public async Task<ActionResult<TrunkState?>> ReadState()
        {
            var current = _fileManager.Current;

            if (current == null)
            {
                return TrunkState.Empty();
            }

            return  await _trunkManager.Observable.FirstAsync();
        }
    }
}