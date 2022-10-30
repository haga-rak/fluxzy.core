// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy
{
    public interface IAuthority
    {
        string HostName { get; }

        int Port { get; }

        bool Secure { get; }
    }
}
