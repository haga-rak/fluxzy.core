// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using Fluxzy.Readers;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController
    {
        private readonly IObservable<IArchiveReader> _archiveReaderObservable;

        public ConnectionController(IObservable<IArchiveReader> archiveReaderObservable)
        {
            _archiveReaderObservable = archiveReaderObservable;
        }

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ConnectionInfo?>> Get(int connectionId)
        {
            var archiveReader = await _archiveReaderObservable.FirstAsync();
            return archiveReader.ReadConnection(connectionId);
        }
    }
}