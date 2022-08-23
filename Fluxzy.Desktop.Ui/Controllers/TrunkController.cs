// Copyright © 2022 Haga Rakotoharivelo

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

        public TrunkController(TrunkManager trunkManager)
        {
            _trunkManager = trunkManager;
        }

        [HttpPost("read")]
        public async Task<ExchangeState> ReadState([FromBody] ExchangeBrowsingState browsingState)
        {
            return await _trunkManager.ReadState(browsingState); 
        }
    }
}