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
            var a = client.LogProfiles.List();
            var b = client.Operations.List();
        }

        [Test]
        public void Test01_AppServicePlanSettings()
        {
            //Assert.AreEqual("southeastasia", applicationInsight.Location);
            //Assert.AreEqual("other", applicationInsight.ApplicationType);
            //Assert.AreEqual(30, applicationInsight.RetentionInDays);
        }
    }
}
