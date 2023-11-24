// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    internal class SystemProxyRegistration : IAsyncDisposable
    {
        private readonly SystemProxyRegistrationManager _instance;
        internal SystemProxyRegistration(SystemProxyRegistrationManager instance)
        {
            _instance = instance;
        }
        
        public async ValueTask DisposeAsync()
        {
            await _instance.UnRegister(); 
        }
    }
}
