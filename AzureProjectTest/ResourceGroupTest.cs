using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureProjectTest.Helper;
using NUnit.Framework;

namespace AzureProjectTest
{
    [GameClass(1)]
    class ResourceGroupTest
    {
        private ArmClient armClient;
        private ResourceGroupData rg;

        public ResourceGroupTest()
        {
            Setup();
        }

        [SetUp]
        public async Task Setup()
        {
            var config = new Config();
            armClient = new ArmClient(config.ClientSecretCredential, config.SubscriptionId);
            var subscription = await armClient.GetDefaultSubscriptionAsync();
            rg = subscription.GetResourceGroups().Get(Constants.ResourceGroupName).Value.Data;
        }

        [GameTask("Can you help create a resource group named 'projProd' in Hong Kong?", 2, 10, 1)]
        [Test]
        public async Task Test01_ResourceGroupExist()
        {
            Assert.IsNotNull(rg);
        }

        [GameTask(1)]
        [Test]
        public async Task Test02_ResourceGroupLocation()
        {
            Assert.AreEqual(AzureLocation.EastAsia, rg.Location);
        }
    }
}
