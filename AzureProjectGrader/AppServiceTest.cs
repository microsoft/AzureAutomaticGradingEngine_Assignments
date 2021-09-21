
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Storage.Models;

namespace AzureProjectGrader
{
    class AppServiceTest
    {
        private IAppServiceManager client;
        private IAppServicePlan appServicePlan;
        private IFunctionApp functionApp;
        private StorageAccount storageAccount;

        [SetUp]
        public void Setup()
        {
            var config = new Config();
            client = AppServiceManager.Configure().Authenticate(config.Credentials, config.SubscriptionId);
            appServicePlan = client.AppServicePlans.ListByResourceGroup(Constants.ResourceGroupName).FirstOrDefault(c => c.Tags.ContainsKey("key") && c.Tags["key"] == "AppServicePlan");
            functionApp = client.FunctionApps.ListByResourceGroup(Constants.ResourceGroupName).FirstOrDefault(c => c.Tags.ContainsKey("key") && c.Tags["key"] == "FunctionAppApp");

            var storageAccountTest = new StorageAccountTest();
            storageAccount = storageAccountTest.GetLogicStorageAccount(storageAccountTest.GetStorageAccounts());
            storageAccountTest.TearDown();
        }


        [Test]
        public void Test01_AppServicePlanSettings()
        {
            Assert.AreEqual("southeastasia", appServicePlan.Region.Name);
            Assert.AreEqual("Dynamic", appServicePlan.PricingTier.SkuDescription.Tier);
            Assert.AreEqual("Y1", appServicePlan.PricingTier.SkuDescription.Size);
            Assert.AreEqual("Windows", appServicePlan.OperatingSystem.ToString());
        }

        [Test]
        public void Test02_FunctionAppSettings()
        {
            Assert.AreEqual("southeastasia", functionApp.Region.Name);
            IReadOnlyDictionary<string, IAppSetting> appSettings = functionApp.GetAppSettings();    
            Assert.AreEqual("~3", appSettings["FUNCTIONS_EXTENSION_VERSION"].Value.ToString());
            Assert.AreEqual("node", appSettings["FUNCTIONS_WORKER_RUNTIME"].Value.ToString());
            Assert.AreEqual("~14", appSettings["WEBSITE_NODE_DEFAULT_VERSION"].Value.ToString());
            Assert.That(appSettings["WEBSITE_RUN_FROM_PACKAGE"].Value.ToString(), Does.StartWith($"https://{storageAccount.Name}.blob.core.windows.net/code/"));
            Assert.That(appSettings["WEBSITE_RUN_FROM_PACKAGE"].Value.ToString(), Does.EndWith("app.zip"));
            Assert.That(appSettings["StorageConnectionAppSetting"].Value.ToString(), Does.StartWith($"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey="));
            Assert.That(appSettings["WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"].Value.ToString(), Does.StartWith($"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey="));
            Assert.IsNotNull(appSettings["APPINSIGHTS_INSTRUMENTATIONKEY"].Value);
        }
    }
}
