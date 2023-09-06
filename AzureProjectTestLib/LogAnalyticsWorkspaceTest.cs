using AzureProjectTestLib.Helper;
using Microsoft.Azure.Management.Monitor;
using NUnit.Framework;

namespace AzureProjectTestLib;

[Parallelizable(ParallelScope.Children), Timeout(Constants.Timeout)]
internal class LogAnalyticsWorkspaceTest
{
    private MonitorManagementClient? client;

    public LogAnalyticsWorkspaceTest()
    {
        Setup();
    }

    [SetUp]
    public void Setup()
    {
        var config = new Config();
        client = new MonitorManagementClient(config.Credentials, new HttpClient(), true);
        client.SubscriptionId = config.SubscriptionId;
    }
}