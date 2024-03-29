// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Certificates
{
    internal class FluxzySecurityParams
    {
        public static FluxzySecurity Current { get; } = new FluxzySecurity(FluxzySecurity.DefaultCertificatePath, new SystemEnvironmentProvider());

    }
}
