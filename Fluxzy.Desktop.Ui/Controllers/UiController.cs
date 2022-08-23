using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace Echoes.Desktop.Ui.Controllers
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
        public ActionResult<UiState> Get()
        {
            return _stateManager.GetUiState(); 
        }
    }
}
