// Copyright © 2022 Haga Rakotoharivelo

using Microsoft.AspNetCore.Mvc;
using Fluxzy.Formatters;

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
        [HttpGet("request/{exchangeId}")]
        public async Task<List<object>> Get(int exchangeId)
        {
            // TODO : this action lacks elegance. chore refactor.

            var results = new List<object>();

            await foreach (var item in _producerFactory.GetRequestFormattedResults(
                               exchangeId))
            {
                results.Add(item);
            }

            return results;
        }
    }
}