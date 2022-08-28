using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Echoes.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UiController : ControllerBase
    {
        private readonly UiStateManager _stateManager;
        private readonly ProxyControl _proxyControl;
        private readonly GlobalFileManager _globalFileManager;
        private readonly FluxzySettingManager _settingHolder;

        public UiController(UiStateManager stateManager, 
            ProxyControl proxyControl, GlobalFileManager globalFileManager, FluxzySettingManager settingHolder)
        {
            _stateManager = stateManager;
            _proxyControl = proxyControl;
            _globalFileManager = globalFileManager;
            _settingHolder = settingHolder;
        }

        // GET: api/<UiController>
        [HttpGet("state")]
        public ActionResult<UiState> Get()
        {
            return _stateManager.GetUiState(); 
        }
    }
}
