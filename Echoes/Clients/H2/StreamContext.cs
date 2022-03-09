using Echoes.Clients.H2.Encoder.Utils;

namespace Echoes.Clients.H2
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
            WindowSizeHolder overallWindowSizeHolder, Http11Parser parser)
        {
            ConnectionId = connectionId;
            Authority = authority;
            Setting = setting;
            Logger = logger;
            HeaderEncoder = headerEncoder;
            UpStreamChannel = upStreamChannel;
            OverallWindowSizeHolder = overallWindowSizeHolder;
            Parser = parser;
        }

        public int ConnectionId { get; }

        public Authority Authority { get; }

        public H2StreamSetting Setting { get; }

        public H2Logger Logger { get; }

        public IHeaderEncoder HeaderEncoder { get; }

        public UpStreamChannel UpStreamChannel { get; }

        public WindowSizeHolder OverallWindowSizeHolder { get; }

        public Http11Parser Parser { get;  }
    }
}