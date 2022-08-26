// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Echoes.Desktop.Ui.Controllers
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
        public async Task<ExchangeState> ReadState([FromBody] ExchangeBrowsingState browsingState)
        {
            var current = _globalFileManager.Current;

            if (current == null)
            {
                return ExchangeState.Empty();
            }

            return await _trunkManager.ReadState(current, browsingState); 
        }
    }
}