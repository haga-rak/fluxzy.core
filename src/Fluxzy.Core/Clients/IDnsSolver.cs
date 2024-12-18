// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Clients
{
    public interface IDnsSolver
    {
        Task<IPAddress> SolveDns(string hostName);
    }
}
