// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;

namespace Fluxzy.Desktop.Services.Models
{
    public class ProxyState
    {
        public ProxyState(string errorMessage)
        {
            OnError = true;
            BoundConnections = new List<ProxyEndPoint>();
            Message = errorMessage;
            RunSettings = null;
        }

        public ProxyState(FluxzySetting setting, IEnumerable<IPEndPoint> endPoints)
        {
            BoundConnections = endPoints
                               .Select(b => new ProxyEndPoint(b.Address.ToString(), b.Port))
                               .ToList();

            var sslConfig = SslConfig.NoSsl;

            if (!setting.GlobalSkipSslDecryption)
                sslConfig = setting.UseBouncyCastle ? SslConfig.BouncyCastle : SslConfig.OsDefault;

            var rawCaptureMode = RawCaptureMode.None;

            if (setting.CaptureRawPacket)
                rawCaptureMode = setting.OutOfProcCapture ? RawCaptureMode.OutProcess : RawCaptureMode.InProcess;

            RunSettings = new ProxyNetworkState(sslConfig, rawCaptureMode);
        }

        public List<ProxyEndPoint> BoundConnections { get; }

        public bool OnError { get; }

        public string? Message { get; }

        public ProxyNetworkState? RunSettings { get; }
    }

    public class ProxyNetworkState
    {
        public ProxyNetworkState(SslConfig sslConfig, RawCaptureMode rawCaptureMode)
        {
            SslConfig = sslConfig;
            RawCaptureMode = rawCaptureMode;
        }

        public SslConfig SslConfig { get; }

        public RawCaptureMode RawCaptureMode { get; }
    }

    public enum SslConfig
    {
        NoSsl = 1,
        OsDefault = 2,
        BouncyCastle
    }

    public enum RawCaptureMode
    {
        None = 1,
        InProcess = 2,
        OutProcess = 3
    }
}
