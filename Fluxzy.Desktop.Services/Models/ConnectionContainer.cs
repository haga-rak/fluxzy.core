// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class ConnectionContainer
    {
        public ConnectionContainer(ConnectionInfo connectionInfo)
        {
            ConnectionInfo = connectionInfo;
            Id = connectionInfo.Id;
        }

        public int Id { get; }

        public ConnectionInfo ConnectionInfo { get; }
    }
}
