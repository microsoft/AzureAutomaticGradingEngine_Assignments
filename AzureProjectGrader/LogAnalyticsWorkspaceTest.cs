using Microsoft.Azure.Management.Monitor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AzureProjectGrader
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
