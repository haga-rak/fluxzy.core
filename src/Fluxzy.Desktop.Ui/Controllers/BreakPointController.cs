// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Breakpoints;
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

        [HttpPost("delete")]
        public ActionResult<bool> DeleteMultipleFilter([FromBody] Guid[] filterIds)
        {
            _handler.DeleteBreakPoints(filterIds);

            return true;
        }

        [HttpDelete("delete/all")]
        public ActionResult<bool> DeleteAllBreakPoints()
        {
            _handler.DeleteAllBreakPoints();

            return true;
        }

        [HttpDelete("clear")]
        public ActionResult<bool> DeleteAll()
        {
            _handler.DeleteAllBreakPoints();
            _handler.ContinueAll();

            return true;
        }

        [HttpDelete("clear-done")]
        public ActionResult<bool> DeleteAllDone()
        {
            _handler.ClearAllDone();

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

        [HttpPost("{exchangeId}/continue/until/{location}")]
        public ActionResult<bool> ContinueExchangeUntilEnd(int exchangeId, BreakPointLocation location)
        {
            _handler.ContinueExchangeUntil(exchangeId, location);

            return true;
        }

        [HttpPost("{exchangeId}/continue/once")]
        public ActionResult<bool> ContinueExchangeOnce(int exchangeId)
        {
            _handler.ContinueExchangeOnce(exchangeId);

            return true;
        }

        [HttpPost("{exchangeId}/endpoint")]
        public ActionResult<bool> SetConnectionSetupStep(
            int exchangeId, [FromBody] ConnectionSetupStepModel connectionSetupStepModel)
        {
            _handler.SetEndPoint(exchangeId, connectionSetupStepModel);

            return true;
        }

        [HttpPost("{exchangeId}/endpoint/continue")]
        public ActionResult<bool> ContinueEndPoint(int exchangeId)
        {
            _handler.ContinueEndPoint(exchangeId);

            return true;
        }

        [HttpPost("{exchangeId}/request")]
        public ActionResult<bool> SetRequest(int exchangeId, [FromBody] RequestSetupStepModel requestSetupStepModel)
        {
            _handler.SetRequest(exchangeId, requestSetupStepModel);

            return true;
        }

        [HttpPost("{exchangeId}/request/continue")]
        public ActionResult<bool> ContinueRequest(int exchangeId)
        {
            _handler.ContinueRequest(exchangeId);

            return true;
        }

        [HttpPost("{exchangeId}/response")]
        public ActionResult<bool> SetResponse(int exchangeId, [FromBody] ResponseSetupStepModel model)
        {
            _handler.SetResponse(exchangeId, model);

            return true;
        }

        [HttpPost("{exchangeId}/response/continue")]
        public ActionResult<bool> ContinueResponse(int exchangeId)
        {
            _handler.ContinueResponse(exchangeId);

            return true;
        }
    }
}
