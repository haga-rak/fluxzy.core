using System.Collections.Immutable;
using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/context-menu")]
    [ApiController]
    public class ContextMenuController : ControllerBase
    {
        private readonly ContextMenuActionProvider _actionProvider;

        public ContextMenuController(ContextMenuActionProvider actionProvider)
        {
            _actionProvider = actionProvider;
        }

        [HttpGet("{exchangeId}")]
        public async Task<ImmutableList<ContextMenuAction>?> Get(int exchangeId)
        {
            return await _actionProvider.GetActions(exchangeId); 
        }
    }
}