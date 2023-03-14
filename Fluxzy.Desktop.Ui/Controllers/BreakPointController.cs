// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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

        [HttpDelete("{filterId}")]
        public ActionResult<bool> Delete(Guid filterId)
        {
            _handler.DeleteBreakPoint(filterId);

            return true; 
        }

        [HttpPost("clear")]
        public ActionResult<bool> DeleteAll()
        {
            _handler.DeleteAllBreakPoints();
            return true; 
        }
    }
}
