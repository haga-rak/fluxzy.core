// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Ui.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly FileManager _fileManager;
        private readonly UiStateManager _uiStateManager;

        public FileController(FileManager fileManager, UiStateManager uiStateManager )
        {
            _fileManager = fileManager;
            _uiStateManager = uiStateManager;
        }

        [HttpPost("new")]
        public async Task<ActionResult<UiState>> New()
        {
            await _fileManager.New();
            return await _uiStateManager.GetUiState();
        }

        [HttpPost("open")]
        public async Task<ActionResult<UiState>> Open(FileOpeningViewModel model)
        {
            await _fileManager.Open(model.FileName);
            return await _uiStateManager.GetUiState();
        }

        [HttpPost("save")]
        public async Task<ActionResult<UiState>> Save([FromServices] IObservable<TrunkState> trunkStateObservable)
        {
            var trunkState = await trunkStateObservable.FirstAsync(); 
            await _fileManager.Save(trunkState);
            return await _uiStateManager.GetUiState();
        }

        [HttpPost("save-as")]
        public async Task<ActionResult<UiState>> SaveAs(FileSaveViewModel model, [FromServices] IObservable<TrunkState> trunkStateObservable)
        {
            var trunkState = await trunkStateObservable.FirstAsync();
            await _fileManager.SaveAs(trunkState, model.FileName);
            return await _uiStateManager.GetUiState();
        }
    }
}