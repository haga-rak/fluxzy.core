// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/forward-message")]
    [ApiController]
    public class ForwardMessageController : ControllerBase
    {
        private readonly ForwardMessageManager _manager;

        public ForwardMessageController(ForwardMessageManager manager)
        {
            _manager = manager;
        }

        [HttpPost("consume")]
        public async Task<ActionResult<List<ForwardMessage>>> Consume()
        {
            return await _manager.ReadAll();
        }
    }
}