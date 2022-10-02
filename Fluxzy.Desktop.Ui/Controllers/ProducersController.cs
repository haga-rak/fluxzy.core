// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Models;
using Fluxzy.Screeners;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;
using Fluxzy.Formatters;
using Fluxzy.Readers;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducersController : ControllerBase
    {
        private readonly ProducerFactory _producerFactory;
        private readonly IObservable<FileState> _fileStateObservable;
        private readonly ProducerSettings _producerSettings;

        public ProducersController(ProducerFactory producerFactory, IObservable<FileState> fileStateObservable,
            ProducerSettings producerSettings)
        {
            _producerFactory = producerFactory;
            _fileStateObservable = fileStateObservable;
            _producerSettings = producerSettings;
        }

        // GET: api/<UiController>
        // Note : object result because System.Text.Json does not handle serializing derived 
        // class from global settings
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        [HttpGet("request/{exchangeId}")]
        public async Task<object> Get(int exchangeId)
        {
            var fileState = await _fileStateObservable.FirstAsync();
            var archiver = new DirectoryArchiveReader(fileState.WorkingDirectory);

            var result = _producerFactory.GetRequestFormattedResults(
                exchangeId, archiver, _producerSettings).ToList();

            return (object) result.OfType<object>(); 
        }
    }
}