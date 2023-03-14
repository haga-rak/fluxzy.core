// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Desktop.Services.BreakPoints;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/breakpoint")]
    [ApiController]
    public class BreakPointController
    {
        private readonly BreakPointHandler _handler;

        public BreakPointController(BreakPointHandler handler)
        {
            _handler = handler;
        }

        [HttpGet]
        public ActionResult<List<Rule>> GetActiveBreakPoints()
        {
            return _handler.GetActiveBreakPoints();
        }

        [HttpPost]
        public ActionResult<bool> Add(Filter filter)
        {
            _handler.AddBreakPoint(filter);

            return true;
        }

        [HttpPost("all")]
        public ActionResult<bool> BreakAll()
        {
            _handler.BreakAll();
            return true;
        }

        [HttpDelete("{filterId}")]
        public ActionResult<bool> Delete(Guid filterId)
        {
            _handler.DeleteBreakPoint(filterId);

            return true;
        }

        [HttpDelete("clear")]
        public ActionResult<bool> DeleteAll()
        {
            _handler.ContinueAll();
            _handler.DeleteAllBreakPoints();

            return true;
        }

        [HttpPost("continue-all")]
        public ActionResult<bool> ContinueAll()
        {
            _handler.ContinueAll();

            return true;
        }

        [HttpPost("{exchangeId}/continue")]
        public ActionResult<bool> ContinueExchangeUntilEnd(int exchangeId)
        {
            _handler.ContinueExchangeUntilEnd(exchangeId);

            return true;
        }


        [HttpPost("{exchangeId}/continue/once")]
        public ActionResult<bool> ContinueExchangeOnce(int exchangeId)
        {
            _handler.ContinueExchangeOnce(exchangeId);

            return true;
        }

        [HttpPost("{exchangeId}/endpoint")]
        public ActionResult<bool> SetEndPoint(int exchangeId, [FromBody] IPEndPoint ipEndPoint)
        {
            _handler.SetEndPoint(exchangeId, ipEndPoint.Address, ipEndPoint.Port);

            return true;
        }

        [HttpPost("{exchangeId}/endpoint/continue")]
        public ActionResult<bool> ContinueEndPoint(int exchangeId)
        {
            _handler.ContinueEndPoint(exchangeId);

            return true;
        }
    }
}
