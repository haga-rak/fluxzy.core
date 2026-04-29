// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluxzy.Clients.H2
{
    internal class StreamContext
    {
        public StreamContext(
            int connectionId,
            Authority authority,
            H2StreamSetting setting,
            IHeaderEncoder headerEncoder,
            UpStreamChannel upStreamChannel,
            WindowSizeHolder overallWindowSizeHolder,
            ILogger? logger = null)
        {
            ConnectionId = connectionId;
            Authority = authority;
            Setting = setting;
            HeaderEncoder = headerEncoder;
            UpStreamChannel = upStreamChannel;
            OverallWindowSizeHolder = overallWindowSizeHolder;
            Logger = logger ?? NullLogger.Instance;
        }

        public int ConnectionId { get; }

        public Authority Authority { get; }

        public H2StreamSetting Setting { get; }

        public IHeaderEncoder HeaderEncoder { get; }

        public UpStreamChannel UpStreamChannel { get; }

        public WindowSizeHolder OverallWindowSizeHolder { get; }

        public ILogger Logger { get; }
    }
}
