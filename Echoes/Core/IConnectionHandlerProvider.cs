namespace Echoes.Core
{
    public interface IConnectionHandlerProvider
    {
        IConnectionHandler GetHandler(); 
    }
}