using System.Linq;
using System.Threading.Tasks;
using Stratis.Bitcoin.Features.PoA.IntegrationTests.Common;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Xunit;

namespace Stratis.Bitcoin.Features.PoA.IntegrationTests
{
    public class AddressIndexerTests
    {
        private string walletName = "mywallet";

        [Fact]
        public async Task IndexedCorrectly()
        {
            var network = new TestPoANetwork();
            PoANodeBuilder builder = PoANodeBuilder.CreatePoANodeBuilder(this);
            builder.WithLogsEnabled();

            CoreNode node1 = builder.CreatePoANode(network, network.FederationKey1, new []{"-addressindex"} ).WithWallet("pass", this.walletName).Start();

            IWalletManager walletManager = node1.FullNode.NodeService<IWalletManager>();
            long balanceOnStart = walletManager.GetBalances(this.walletName, "account 0").Sum(x => x.AmountConfirmed);
            Assert.Equal(0, balanceOnStart);

            long toMineCount = network.Consensus.PremineHeight + network.Consensus.CoinbaseMaturity + 1 - node1.GetTip().Height;
            await node1.MineBlocksAsync((int)toMineCount).ConfigureAwait(false);

            long balanceAfterPremine = walletManager.GetBalances(this.walletName, "account 0").Sum(x => x.AmountConfirmed);

            Assert.Equal(network.Consensus.PremineReward.Satoshi, balanceAfterPremine);


            CoreNode node2 = builder.CreatePoANode(network, network.FederationKey1, new[] { "-addressindex" }).WithWallet("pass", this.walletName).Start();

            await node2.MineBlocksAsync((int)toMineCount*2).ConfigureAwait(false);

            TestHelper.Connect(node1, node2);

            //TestHelper.ConnectAndSync(node1, node2);

            var balanceAfterReorg = walletManager.GetBalances(this.walletName, "account 0").Sum(x => x.AmountConfirmed);
            Assert.Equal(0, balanceAfterReorg);






            builder.Dispose();
        }

    }
}
