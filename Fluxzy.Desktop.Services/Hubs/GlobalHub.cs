using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services.Hubs
{
    public class GlobalHub : Hub
    {
        private readonly UiStateManager _uiStateManager;

        public GlobalHub(UiStateManager uiStateManager)
        {
            _uiStateManager = uiStateManager;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var currentState = await _uiStateManager.GetUiState();

            if (currentState != null)
                await Clients.Caller.SendAsync("uiUpdate", currentState); 
        }
    }
}
