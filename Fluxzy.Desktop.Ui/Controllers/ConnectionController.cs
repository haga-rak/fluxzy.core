// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;

        public ConnectionController(IArchiveReaderProvider archiveReaderProvider)
        {
            _archiveReaderProvider = archiveReaderProvider;
        }

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ConnectionInfo?>> Get(int connectionId)
        {
            var archiveReader = await _archiveReaderProvider.Get();
            return  archiveReader?.ReadConnection(connectionId);
        }
    }
}