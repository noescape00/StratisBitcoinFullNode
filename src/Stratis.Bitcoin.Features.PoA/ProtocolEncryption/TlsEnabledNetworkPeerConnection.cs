using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.AsyncWork;
using Stratis.Bitcoin.P2P.Peer;
using Stratis.Bitcoin.P2P.Protocol;
using Stratis.Bitcoin.P2P.Protocol.Payloads;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.PoA.ProtocolEncryption
{
    public class TlsEnabledNetworkPeerConnection : NetworkPeerConnection
    {
        private readonly CertificatesManager certManager;

        public TlsEnabledNetworkPeerConnection(Network network, INetworkPeer peer, TcpClient client, int clientId,
            ProcessMessageAsync<IncomingMessage> processMessageAsync, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory, PayloadProvider payloadProvider, IAsyncProvider asyncProvider, CertificatesManager certManager) : base(network, peer, client, clientId,
            processMessageAsync, dateTimeProvider, loggerFactory, payloadProvider, asyncProvider)
        {
            this.certManager = certManager;
        }

        protected override void SetStream()
        {
            var clientCertificateCollection = new X509CertificateCollection(new X509Certificate[] { this.certManager.ClientCertificate });

            var sslStream = new SslStream(this.tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(this.certManager.ValidateCertificate), null);

            try
            {
                sslStream.AuthenticateAsClient("servName", clientCertificateCollection, SslProtocols.Tls12, false);
            }
            catch (AuthenticationException e)
            {
                this.logger.LogDebug("Exception: {0}", e.Message);

                if (e.InnerException != null)
                    this.logger.LogDebug("Inner exception: {0}", e.InnerException.Message);

                this.logger.LogTrace("(-)[AUTH_FAILED]");
                return;
            }

            this.stream = sslStream;
        }
    }
}
