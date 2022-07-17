// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Desktop.Service
{
    public class UiStateManager
    {

    }

    public interface IProxyControl
    {
        Task<bool> SetAsSystemProxy();

        Task<bool> UnsetAsSystemProxy();

        Task<bool> SetListening(); 

        Task<bool> UnsetListening(); 
    }
}