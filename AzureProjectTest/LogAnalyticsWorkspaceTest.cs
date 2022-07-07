using Microsoft.Azure.Management.Monitor;
using NUnit.Framework;
using AzureProjectTest.Helper;

namespace AzureProjectTest
{
    [Parallelizable(scope: ParallelScope.Children)]
    class LogAnalyticsWorkspaceTest
    {
        private MonitorManagementClient client;

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
}
