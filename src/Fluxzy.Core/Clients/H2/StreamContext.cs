// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Clients.H2
{
    internal class StreamContext
    {
        public StreamContext(
            int connectionId,
            Authority authority,
            H2StreamSetting setting,
            H2Logger logger,
            IHeaderEncoder headerEncoder,
            UpStreamChannel upStreamChannel,
            WindowSizeHolder overallWindowSizeHolder)
        {
            ConnectionId = connectionId;
            Authority = authority;
            Setting = setting;
            Logger = logger;
            HeaderEncoder = headerEncoder;
            UpStreamChannel = upStreamChannel;
            OverallWindowSizeHolder = overallWindowSizeHolder;
        }

        public int ConnectionId { get; }

        public Authority Authority { get; }

        public H2StreamSetting Setting { get; }

        public H2Logger Logger { get; }

        public IHeaderEncoder HeaderEncoder { get; }

        public UpStreamChannel UpStreamChannel { get; }

        public WindowSizeHolder OverallWindowSizeHolder { get; }
    }
}
