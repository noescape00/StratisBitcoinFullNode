using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Tests.Common;

namespace Stratis.Bitcoin.Features.PoA.IntegrationTests.Common
{
    public class PoANodeBuilder : NodeBuilder
    {
        public EditableTimeProvider TimeProvider { get; }

        private PoANodeBuilder(string rootFolder) : base(rootFolder)
        {
            this.TimeProvider = new EditableTimeProvider();
        }

        public static PoANodeBuilder CreatePoANodeBuilder(object caller, [CallerMemberName] string callingMethod = null)
        {
            string testFolderPath = TestBase.CreateTestDir(caller, callingMethod);
            PoANodeBuilder builder = new PoANodeBuilder(testFolderPath);
            builder.WithLogsDisabled();

            return builder;
        }

        public CoreNode CreatePoANode(PoANetwork network)
        {
            return this.CreateNode(new PoANodeRunner(this.GetNextDataFolderName(), network, this.TimeProvider), "poa.conf");
        }

        public CoreNode CreatePoANode(PoANetwork network, Key key, string[] args = null)
        {
            string dataFolder = this.GetNextDataFolderName();
            CoreNode node = this.CreateNode(new PoANodeRunner(dataFolder, network, this.TimeProvider), "poa.conf");

            var arguments = new List<string>();
            if (args != null)
                arguments.AddRange(args);

            arguments.AddRange(new string[] { "-conf=poa.conf", "-datadir=" + dataFolder });

            var settings = new NodeSettings(network, args: arguments.ToArray());
            var tool = new KeyTool(settings.DataFolder);
            tool.SavePrivateKey(key);

            return node;
        }
    }
}
