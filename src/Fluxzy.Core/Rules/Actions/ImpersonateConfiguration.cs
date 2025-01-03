// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Rules.Actions
{
    public class ImpersonateConfiguration
    {
        public string Ja3FingerPrint { get;  }

        public List<ImpersonateHeader> Headers { get; }

        public int H2WindowUpdate { get; }
    }

    public class ImpersonateH2Setting
    {
        public int ? WindowUpdateSize { get;  }

        public int ? StreamWeight { get;  }

        public int ? StreamExclusive { get;  }
    }
    
    public class ImpersonateHeader
    {
        public string Name { get; }

        public string Value { get; }
    }
}
