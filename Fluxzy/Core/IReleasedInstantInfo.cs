using System;

namespace Echoes.Core
{
    public interface IReleasedInstantInfo
    {
        DateTime ExpireInstant { get; set;  }
    }
}