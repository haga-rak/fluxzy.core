// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class ProxyFilterSetting
    {
        public Dictionary<Guid, Filter> Filters { get; set; } = new(); 
    }

    public class ProxyArchiveFilter
    {
        public Guid ? ArchiveFilter { get; set; }
    }

    public class ClientCertificateStrategy
    {

    }

    public class ClientCertificateStrategyFilter
    {

    }
}