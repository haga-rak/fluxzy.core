// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Bulk.BcCli
{
    public class FluxzyClientProtocol : TlsClientProtocol
    {
        private readonly NssLogWriter _logWritter;
        private TlsSecret _localSecret;

        public FluxzyClientProtocol(Stream stream, NssLogWriter logWritter)
            : base(stream)
        {
            _logWritter = logWritter;
        }

        public SecurityParameters PlainSecurityParameters => Context.SecurityParameters;
        
        protected override void Handle13HandshakeMessage(short type, HandshakeMessageInput buf)
        {
            base.Handle13HandshakeMessage(type, buf);
            
            // Here we shall extract the application keys    

            var alreadyUsed = PlainSecurityParameters.TrafficSecretClient == _localSecret;

            if (!alreadyUsed) {


                _logWritter.Write(NssLogWriter.CLIENT_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());

                _logWritter.Write(NssLogWriter.SERVER_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretServer.ExtractKeySilently());
                
                if (PlainSecurityParameters.ExporterMasterSecret != null)
                _logWritter.Write(NssLogWriter.EXPORTER_SECRET, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.ExporterMasterSecret.ExtractKeySilently());
            }
            
            

        }

        protected override void Process13ServerHelloCoda(ServerHello serverHello, bool afterHelloRetryRequest)
        {
            base.Process13ServerHelloCoda(serverHello, afterHelloRetryRequest);

            // Here we shall extract the handshake keys    

            _localSecret = PlainSecurityParameters.TrafficSecretClient; 

            _logWritter.Write(NssLogWriter.CLIENT_HANDSHAKE_TRAFFIC_SECRET, PlainSecurityParameters.ClientRandom,
                PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());
            
            _logWritter.Write(NssLogWriter.SERVER_HANDSHAKE_TRAFFIC_SECRET, PlainSecurityParameters.ClientRandom,
                PlainSecurityParameters.TrafficSecretServer.ExtractKeySilently());
        }
    }
}