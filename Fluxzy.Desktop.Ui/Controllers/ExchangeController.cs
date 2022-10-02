// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController
    {
        public record SaveFileViewModel(string FileName); 

        private readonly ProducerFactory _producerFactory;
        public ExchangeController(
            ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        [HttpPost("{exchangeId}/save-request-body")]
        public async Task<ActionResult<bool>> SaveRequestBody(
            int exchangeId,
            [FromBody] SaveFileViewModel body,
            [FromServices] SaveRequestBodyProducerAction action)
        {
            return await action.Do(exchangeId, body.FileName);
        }

    }
}