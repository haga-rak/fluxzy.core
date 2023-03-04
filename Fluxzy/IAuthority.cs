// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    public interface IAuthority
    {
        string HostName { get; }

        int Port { get; }

        bool Secure { get; }
    }
}
