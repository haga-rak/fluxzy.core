using System;

namespace Fluxzy.Core
{
    public interface IReleasedInstantInfo
    {
        DateTime ExpireInstant { get; set;  }
    }
}