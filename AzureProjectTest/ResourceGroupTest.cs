using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureProjectTest.Helper;
using NUnit.Framework;

namespace AzureProjectTest;

[GameClass(1), Timeout(Constants.TIMEOUT)]
internal class ResourceGroupTest
{
    private ArmClient? armClient;
    private ResourceGroupData? rg;

    public ResourceGroupTest()
    {
        Setup().ConfigureAwait(false);
    }

    [SetUp]
    public async Task Setup()
    {
        var config = new Config();
        armClient = new ArmClient(config.ClientSecretCredential, config.SubscriptionId);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        rg = (await subscription.GetResourceGroups()!.GetAsync(Constants.ResourceGroupName)).Value.Data;
    }

    [GameTask("Can you create a resource group named 'projProd' in Singapore?", 2, 10, 1)]
    [Test]
    public void Test01_ResourceGroupExist()
    {
        Assert.IsNotNull(rg);
    }

    [GameTask(1)]
    [Test]
    public void Test02_ResourceGroupLocation()
    {
        Assert.AreEqual(AzureLocation.EastAsia, rg!.Location);
    }
}