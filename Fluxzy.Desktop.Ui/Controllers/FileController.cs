// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Services.Ui;
using Fluxzy.Desktop.Ui.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly FileManager _fileManager;
        private readonly UiStateManager _uiStateManager;

        public FileController(FileManager fileManager, UiStateManager uiStateManager)
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
        public async Task<ActionResult<UiState>> Open(
            FileOpeningViewModel model, [FromServices] LastOpenFileManager lastOpenFileManager)
        {
            await _fileManager.Open(model.FileName);
            lastOpenFileManager.Add(model.FileName);

            return await _uiStateManager.GetUiState();
        }

        [HttpPost("opening-request")]
        public ActionResult<bool> OpeningRequest(
            FileOpeningRequestViewModel model,
            [FromServices]
            ForwardMessageManager forwardMessageManager)
        {
            forwardMessageManager.Send(model);

            return true;
        }

        [HttpPost("save")]
        public async Task<ActionResult<UiState>> Save([FromServices] IObservable<TrunkState> trunkStateObservable)
        {
            var trunkState = await trunkStateObservable.FirstAsync();
            await _fileManager.Save(trunkState);

            return await _uiStateManager.GetUiState();
        }

        [HttpPost("save-as")]
        public async Task<ActionResult<UiState>> SaveAs(
            FileSaveViewModel model, [FromServices] IObservable<TrunkState> trunkStateObservable,
            [FromServices]
            LastOpenFileManager lastOpenFileManager)
        {
            var trunkState = await trunkStateObservable.FirstAsync();
            await _fileManager.SaveAs(trunkState, model.FileName);
            lastOpenFileManager.Add(model.FileName);

            return await _uiStateManager.GetUiState();
        }

        [HttpPost("export/har")]
        public async Task<ActionResult<bool>> ExportHar(HarExportRequest model)
        {
            return await _fileManager.ExportHttpArchive(model);
        }

        [HttpPost("export/saz")]
        public async Task<ActionResult<bool>> ExportSaz(SazExportRequest model)
        {
            return await _fileManager.ExportSaz(model);
        }
    }
}
