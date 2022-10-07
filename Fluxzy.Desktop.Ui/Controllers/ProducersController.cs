// Copyright © 2022 Haga Rakotoharivelo

using Microsoft.AspNetCore.Mvc;
using Fluxzy.Formatters;
using Fluxzy.Desktop.Ui.ViewModels;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducersController : ControllerBase
    {
        private readonly ProducerFactory _producerFactory;

        public ProducersController(
            ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        // GET: api/<UiController>
        // Note : object result because System.Text.Json does not handle serializing derived 
        // class from global settings
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        [HttpGet("formatters/{exchangeId}")]
        public async Task<ActionResult<FormatterContainerViewModelGeneric>> GetFormatters(int exchangeId)
        {
            var context = await _producerFactory.GetProducerContext(exchangeId);

            if (context == null)
                return NotFound();

            var viewModel = new FormatterContainerViewModel(_producerFactory.GetRequestFormattedResults(
                exchangeId, context).ToList(), _producerFactory.GetResponseFormattedResults(
                exchangeId, context).ToList());

            return new FormatterContainerViewModelGeneric(viewModel);
        }
    }
}