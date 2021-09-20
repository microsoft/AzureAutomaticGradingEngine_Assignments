using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using NUnit.Framework;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AzureProjectGrader
{
    class StorageAccountTest
    {
        private StorageManagementClient client;
        private StorageAccount storageAccount;
        private StorageAccount webStorageAccount;

        private static readonly HttpClient httpClient = new HttpClient();

        [SetUp]
        public void Setup()
        {
            var config = new Config();
            client = new StorageManagementClient(config.Credentials);
            client.SubscriptionId = config.SubscriptionId;

            var storageAccounts = client.StorageAccounts.ListByResourceGroup(Constants.ResourceGroupName);
            storageAccount = storageAccounts.FirstOrDefault(c => c.Name.Contains("sa"));
            webStorageAccount = storageAccounts.FirstOrDefault(c => c.Name.Contains("web"));
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

    }
}
