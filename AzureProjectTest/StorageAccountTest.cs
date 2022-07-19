using AzureProjectTest.Helper;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Rest.Azure;
using NUnit.Framework;

namespace AzureProjectTest;

[GameClass(2)]
internal class StorageAccountTest
{
    private static readonly HttpClient httpClient = new();
    private StorageManagementClient client;
    private StorageAccount storageAccount;
    private StorageAccount webStorageAccount;

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


        var storageAccounts = GetStorageAccounts();
        storageAccount = GetLogicStorageAccount(storageAccounts);
        webStorageAccount =
            storageAccounts.FirstOrDefault(c => c.Tags.ContainsKey("usage") && c.Tags["usage"] == "StaticWeb");
    }

    public StorageAccount GetLogicStorageAccount(IPage<StorageAccount> storageAccounts)
    {
        return storageAccounts.FirstOrDefault(c => c.Tags.ContainsKey("usage") && c.Tags["usage"] == "logic");
    }

    public IPage<StorageAccount> GetStorageAccounts()
    {
        return client.StorageAccounts.ListByResourceGroup(Constants.ResourceGroupName);
    }

    public Table GetMessageTable()
    {
        return client.Table.Get(Constants.ResourceGroupName, storageAccount.Name, "message");
    }

    public StorageQueue GetJobQueue()
    {
        return client.Queue.Get(Constants.ResourceGroupName, storageAccount.Name, "job");
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
    }

    [GameTask(
        "Can you help create a Storage account in resource group 'projProd' and add tag name 'usage' and value 'logic'?",
        2, 10)]
    [Test]
    public void Test01_StorageAccountsWithTag()
    {
        Assert.IsNotNull(storageAccount, "StorageAccount Plans with tag {usage:logic}.");
    }

    [GameTask(
        "Can you help create a Storage account in resource group 'projProd' and add tag name 'usage' and value 'StaticWeb'?",
        2, 10)]
    [Test]
    public void Test02_StorageAccountsWithTag()
    {
        Assert.IsNotNull(webStorageAccount, "Static Web StorageAccount Plans with tag {usage:StaticWeb}.");
    }

    [GameTask(
        "Can you help change your Storage account tagged 'usage' with 'logic' to southeastasia, AccessTier to Hot, StorageV2, Standard_LRS and allow public access?",
        2, 20)]
    [Test]
    public void Test03_StorageAccountSettings()
    {
        Assert.AreEqual("southeastasia", storageAccount.Location);
        Assert.AreEqual("Hot", storageAccount.AccessTier.Value.ToString());
        Assert.AreEqual("StorageV2", storageAccount.Kind);
        Assert.AreEqual("Standard_LRS", storageAccount.Sku.Name);
        Assert.IsTrue(storageAccount.AllowBlobPublicAccess);
    }

    [GameTask(
        "Can you help change your Storage account tagged 'usage' with 'StaticWeb' to eastasia, AccessTier to Hot, StorageV2, Standard_LRS and allow public access?" +
        "I need the index page of text 'This is index page.' and the error page of text 'This is error page.'.", 2, 30)]
    [Test]
    public async Task Test04_WebStorageAccountSettings()
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

        Assert.AreEqual("Response status code does not indicate success: 404 (The requested content does not exist.).",
            ex.Message);

        var response = await httpClient.GetAsync(webUrl + "/PageIsNotExist" + DateTime.Now.Ticks);
        var error = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("This is error page.", error);
    }

    [GameTask("I need a Blog container named 'code' in Storage account tagged 'usage' with 'logic'. Can you help?", 2,
        10)]
    [Test]
    public void Test05_StorageAccountCodeContainer()
    {
        var codeContainer = client.BlobContainers.Get(Constants.ResourceGroupName, storageAccount.Name, "code");
        Assert.IsNotNull(codeContainer);
        Assert.AreEqual("Blob", codeContainer.PublicAccess.Value.ToString());
    }

    [GameTask("I need a Azure table named 'message' in Storage account tagged 'usage' with 'logic'. Can you help?", 2,
        10)]
    [Test]
    public void Test06_StorageAccountMessageTable()
    {
        var messageTable = GetMessageTable();
        Assert.IsNotNull(messageTable);
    }

    [GameTask("I need a Azure Storage Queue named 'job' in Storage account tagged 'usage' with 'logic'. Can you help?",
        2, 10)]
    [Test]
    public void Test07_StorageAccountJobQueue()
    {
        var jobQueue = GetJobQueue();
        Assert.IsNotNull(jobQueue);
    }
}