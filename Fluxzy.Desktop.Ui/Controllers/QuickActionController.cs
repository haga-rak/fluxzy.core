// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.ContextualFilters;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuickActionController : ControllerBase
    {
        private readonly IObservable<QuickActionResult> _quickActionObservable;

        public QuickActionController(IObservable<QuickActionResult> quickActionObservable)
        {
            _quickActionObservable = quickActionObservable;
        }

        [HttpGet("")]
        public async Task<ActionResult<QuickActionResult>> GetQuickActions()
        {
            var result = await _quickActionObservable.FirstAsync();
            return result; 
        }
    }
}
