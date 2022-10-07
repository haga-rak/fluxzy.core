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
        public async Task<FormatterContainerViewModelGeneric> GetFormatters(int exchangeId)
        {
            var requestFormatters = _producerFactory.GetRequestFormattedResults(
                exchangeId).ToListAsync();

            var responseFormatters = _producerFactory.GetResponseFormattedResults(
                exchangeId).ToListAsync();

            var res = await Task.WhenAll(requestFormatters, responseFormatters);

            var viewModel = new FormatterContainerViewModel(res[0], res[1]);

            return new FormatterContainerViewModelGeneric(viewModel);
        }
    }
}