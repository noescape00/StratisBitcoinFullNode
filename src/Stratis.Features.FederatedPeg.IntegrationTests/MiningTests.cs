using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Features.PoA;
using Stratis.Bitcoin.Features.PoA.IntegrationTests.Common;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Tests.Common;
using Stratis.Features.FederatedPeg.IntegrationTests.Utils;
using Stratis.Sidechains.Networks;
using Xunit;

namespace Stratis.Features.FederatedPeg.IntegrationTests
{
    public class MiningTests
    {
        [Fact]
        public void NodeCanLoadFederationKey()
        {
            var network = (CirrusRegTest)CirrusNetwork.NetworksSelector.Regtest();

            using (PoANodeBuilder builder = PoANodeBuilder.CreatePoANodeBuilder(this))
            {
                // Create first node as fed member.
                Key key = network.FederationKeys[0];
                CoreNode node = builder.CreatePoANode(network, key).Start();

                Assert.True(node.FullNode.NodeService<IFederationManager>().IsFederationMember);
                Assert.Equal(node.FullNode.NodeService<IFederationManager>().CurrentFederationKey, key);

                // Create second node as normal node.
                CoreNode node2 = builder.CreatePoANode(network).Start();

                Assert.False(node2.FullNode.NodeService<IFederationManager>().IsFederationMember);
                Assert.Equal(node2.FullNode.NodeService<IFederationManager>().CurrentFederationKey, null);
            }
        }

        [Fact]
        public async Task NodeCanMineAsync()
        {
            var network = (CirrusRegTest)CirrusNetwork.NetworksSelector.Regtest();

            using (PoANodeBuilder builder = PoANodeBuilder.CreatePoANodeBuilder(this))
            {
                CoreNode node0 = builder.CreatePoANode(network, network.FederationKeys[0]).Start();
                CoreNode node1 = builder.CreatePoANode(network, network.FederationKeys[1]).Start();

                TestHelper.Connect(node0, node1);

                await node0.MineBlocksAsync(3);

                TestHelper.WaitForNodeToSync(node0, node1);

                await node0.MineBlocksAsync(2);

                TestHelper.WaitForNodeToSync(node0, node1);

                Assert.Equal(node0.GetTip().HashBlock, node1.GetTip().HashBlock);
            }
        }

        [Fact]
        public async Task PremineIsReceivedAsync()
        {
            var network = (CirrusRegTest)CirrusNetwork.NetworksSelector.Regtest();

            using (PoANodeBuilder builder = PoANodeBuilder.CreatePoANodeBuilder(this))
            {
                string walletName = "mywallet";
                CoreNode node = builder.CreatePoANode(network, network.FederationKeys[0]).WithWallet("pass", walletName).Start();

                IWalletManager walletManager = node.FullNode.NodeService<IWalletManager>();
                long balanceOnStart = walletManager.GetBalances(walletName, "account 0").Sum(x => x.AmountConfirmed);
                Assert.Equal(0, balanceOnStart);

                int mineCount = (int)(network.Consensus.PremineHeight + network.Consensus.CoinbaseMaturity + 1);
                await node.MineBlocksAsync(mineCount);

                long balanceAfterPremine = walletManager.GetBalances(walletName, "account 0").Sum(x => x.AmountConfirmed);

                Assert.Equal(network.Consensus.PremineReward.Satoshi, balanceAfterPremine);
            }
        }
    }
}
