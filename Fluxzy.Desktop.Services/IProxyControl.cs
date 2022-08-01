namespace Fluxzy.Desktop.Services
{
    public interface IProxyControl
    {
        Task<bool> SetAsSystemProxy();

        Task<bool> UnsetAsSystemProxy();

        Task<bool> SetListening(); 

        Task<bool> UnsetListening(); 
    }
}