// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/file-content")]
    [ApiController]
    public class FileContentController : ControllerBase
    {
        private readonly FileManager _fileManager;

        public FileContentController(FileManager fileManager)
        {
            _fileManager = fileManager;
        }

        [HttpPost("read")]
        public async Task<ActionResult<TrunkState?>> ReadState()
        {
            var contentManager = await _fileManager.CurrentContent.FirstAsync();

            return  await contentManager.Observable.FirstAsync();
        }
    }
}