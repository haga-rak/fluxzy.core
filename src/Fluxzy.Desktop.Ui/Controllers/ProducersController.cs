// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Ui.ViewModels;
using Fluxzy.Formatters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducersController : ControllerBase
    {
        private readonly ProducerFactory _producerFactory;

        public ProducersController(ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        // GET: api/<UiController>
        // Note : object result because System.Text.Json does not handle serializing derived 
        // class from global settings
        /// <summary>
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        [HttpGet("formatters/{exchangeId}")]
        public async Task<ActionResult<FormatterContainerViewModelGeneric>> GetFormatters(int exchangeId)
        {
            var producerContext = await _producerFactory.GetProducerContext(exchangeId);

            if (producerContext == null)
                return NotFound();

            var exchangeContext = producerContext.GetContextInfo();

            var viewModel = new FormatterContainerViewModel(_producerFactory.GetRequestFormattedResults(
                exchangeId, producerContext).ToList(), _producerFactory.GetResponseFormattedResults(
                exchangeId, producerContext).ToList(), exchangeContext);

            return new FormatterContainerViewModelGeneric(viewModel, exchangeContext);
        }
    }
}
