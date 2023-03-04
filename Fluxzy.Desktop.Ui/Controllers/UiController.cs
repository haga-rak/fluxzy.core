// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UiController : ControllerBase
    {
        private readonly UiStateManager _stateManager;

        public UiController(UiStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        // GET: api/<UiController>
        [HttpGet("state")]
        public async Task<ActionResult<UiState>> Get()
        {
            return await _stateManager.GetUiState();
        }
    }
}
