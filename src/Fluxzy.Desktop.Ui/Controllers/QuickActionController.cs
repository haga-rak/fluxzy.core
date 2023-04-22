// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.ContextualFilters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuickActionController : ControllerBase
    {
        private readonly QuickActionBuilder _quickActionBuilder;
        private readonly IObservable<QuickActionResult> _quickActionObservable;

        public QuickActionController(
            IObservable<QuickActionResult> quickActionObservable, QuickActionBuilder quickActionBuilder)
        {
            _quickActionObservable = quickActionObservable;
            _quickActionBuilder = quickActionBuilder;
        }

        [HttpGet("")]
        public async Task<ActionResult<QuickActionResult>> GetQuickActions()
        {
            var result = await _quickActionObservable.FirstAsync();

            return result;
        }

        [HttpGet("static")]
        public ActionResult<QuickActionResult> GetQuickActionStatic()
        {
            var result = _quickActionBuilder.GetStaticQuickActions();

            return result;
        }
    }
}
