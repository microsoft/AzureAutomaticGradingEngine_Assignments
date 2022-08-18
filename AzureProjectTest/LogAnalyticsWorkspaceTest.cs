using AzureProjectTest.Helper;
using Microsoft.Azure.Management.Monitor;
using NUnit.Framework;

namespace AzureProjectTest;

[Parallelizable(ParallelScope.Children)]
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