using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CertificateAuthority.Client;
using Microsoft.Extensions.Logging;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.PoA.ProtocolEncryption
{
    public class RevocationChecker : IDisposable
    {
        /*
        * TODO update cache every 12 hours
        */

        private const string kvRepoKey = "revocedcerts";

        private readonly NodeSettings nodeSettings;

        private readonly IKeyValueRepository kvRepo;

        protected ILogger logger;

        private HashSet<string> revokedCertsCashe;

        private Client client;

        public RevocationChecker(NodeSettings nodeSettings, IKeyValueRepository kvRepo, ILoggerFactory loggerFactory)
        {
            this.nodeSettings = nodeSettings;
            this.kvRepo = kvRepo;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public async Task InitializeAsync()
        {
            TextFileConfiguration config = nodeSettings.ConfigReader;
            string certificateAuthorityUrl = config.GetOrDefault<string>("caurl", "https://localhost:5001");

            this.client = new Client(certificateAuthorityUrl, new HttpClient());

            this.revokedCertsCashe = this.kvRepo.LoadValue<HashSet<string>>(kvRepoKey);

            if (this.revokedCertsCashe == null)
                await UpdateRevokedCertsCasheAsync().ConfigureAwait(false);
        }

        public async Task<bool> IsCertificateRevokedAsync(string thumbprint)
        {
            // First try to ask CA server directly.
            try
            {
                var requestModel = new GetCertificateStatusModel() {AsString = false, Thumbprint = thumbprint};
                string status = await this.client.GetCertificateStatusAsync(requestModel).ConfigureAwait(false);

                return status != "Good";
            }
            catch (Exception e)
            {
                this.logger.LogDebug("Error while checking certificate status: '{0}'.", e.ToString());

                // Use cache.
                if ((this.revokedCertsCashe != null) && this.revokedCertsCashe.Contains(thumbprint))
                    return true;
            }

            return false;
        }

        private async Task UpdateRevokedCertsCasheAsync()
        {
            try
            {
                ICollection<string> result = await this.client.GetRevokedCertificatesAsync().ConfigureAwait(false);
                this.revokedCertsCashe = new HashSet<string>(result);
            }
            catch (Exception e)
            {
                this.logger.LogWarning("Failed to reach certificate authority server.");
                this.logger.LogDebug(e.ToString());
            }
        }

        public void Dispose()
        {
            if (this.revokedCertsCashe != null)
                this.kvRepo.SaveValue(kvRepoKey, this.revokedCertsCashe);
        }
    }
}
