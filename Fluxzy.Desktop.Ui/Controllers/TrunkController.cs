// Copyright © 2022 Haga Rakotoharivelo

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
        private readonly GlobalFileManager _globalFileManager;

        public TrunkController(TrunkManager trunkManager, GlobalFileManager globalFileManager)
        {
            _trunkManager = trunkManager;
            _globalFileManager = globalFileManager;
        }

        [HttpPost("read")]
        public ActionResult<TrunkState?> ReadState()
        {
            var current = _globalFileManager.Current;

            if (current == null)
            {
                return TrunkState.Empty();
            }

            return _trunkManager.Current; 
        }
    }
}