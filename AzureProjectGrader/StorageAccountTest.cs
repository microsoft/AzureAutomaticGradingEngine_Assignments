using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using NUnit.Framework;
using System.Net.Http;
using Microsoft.Rest.Azure;

namespace AzureProjectGrader
{
    class StorageAccountTest
    {
        private StorageManagementClient client;
        private StorageAccount storageAccount;
        private StorageAccount webStorageAccount;

        private static readonly HttpClient httpClient = new HttpClient();

        public StorageAccountTest()
        {
            Setup();
        }

        [SetUp]
        public void Setup()
        {
            var config = new Config();
            client = new StorageManagementClient(config.Credentials);
            client.SubscriptionId = config.SubscriptionId;
            IPage<StorageAccount> storageAccounts = GetStorageAccounts();
            storageAccount = GetLogicStorageAccount(storageAccounts);        
            webStorageAccount = storageAccounts.FirstOrDefault(c => c.Tags.ContainsKey("usage") && c.Tags["usage"] == "StaticWeb");
        }

        public StorageAccount GetLogicStorageAccount(IPage<StorageAccount> storageAccounts)
        {
            return storageAccounts.FirstOrDefault(c => c.Tags.ContainsKey("usage") && c.Tags["usage"] == "logic");
        }

        public IPage<StorageAccount> GetStorageAccounts()
        {
            return client.StorageAccounts.ListByResourceGroup(Constants.ResourceGroupName);  
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
        }

        [Test]
        public void Test01_StorageAccountSettings()
        {
            Assert.AreEqual("southeastasia", storageAccount.Location);
            Assert.AreEqual("Hot", storageAccount.AccessTier.Value.ToString());
            Assert.AreEqual("StorageV2", storageAccount.Kind);
            Assert.AreEqual("Standard_LRS", storageAccount.Sku.Name);
            Assert.IsTrue(storageAccount.AllowBlobPublicAccess);
        }

        [Test]
        public async Task Test02_WebStorageAccountSettings()
        {
            Assert.AreEqual("eastasia", webStorageAccount.Location);
            Assert.AreEqual("Hot", webStorageAccount.AccessTier.Value.ToString());
            Assert.AreEqual("StorageV2", webStorageAccount.Kind);
            Assert.AreEqual("Standard_LRS", webStorageAccount.Sku.Name);
            Assert.IsFalse(webStorageAccount.AllowBlobPublicAccess);

            var webContainer = client.BlobContainers.Get(Constants.ResourceGroupName, webStorageAccount.Name, "$web");
            Assert.IsNotNull(webContainer);

            var webUrl = webStorageAccount.PrimaryEndpoints.Web;
            var index = await httpClient.GetStringAsync(webUrl);
            Assert.AreEqual("This is index page.", index);

            var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                var error = await httpClient.GetStringAsync(webUrl + "/PageIsNotExist" + DateTime.Now.Ticks);
            });

            Assert.AreEqual("Response status code does not indicate success: 404 (The requested content does not exist.).", ex.Message);

            HttpResponseMessage response = await httpClient.GetAsync(webUrl + "/PageIsNotExist" + DateTime.Now.Ticks);
            var error = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("This is error page.", error);
        }

        [Test]
        public void Test03_StorageAccountCodeContainer()
        {
            var codeContainer = client.BlobContainers.Get(Constants.ResourceGroupName, storageAccount.Name, "code");
            Assert.IsNotNull(codeContainer);
            Assert.AreEqual("Blob", codeContainer.PublicAccess.Value.ToString());           
        }

        [Test]
        public void Test04_StorageAccountMessageTable()
        {
            var messageTable = client.Table.Get(Constants.ResourceGroupName, storageAccount.Name, "message");
            Assert.IsNotNull(messageTable);          
        }

        [Test]
        public void Test05_StorageAccountJobQueue()
        {
            var jobQueue = client.Queue.Get(Constants.ResourceGroupName, storageAccount.Name, "job");
            Assert.IsNotNull(jobQueue);
        }

    }
}
