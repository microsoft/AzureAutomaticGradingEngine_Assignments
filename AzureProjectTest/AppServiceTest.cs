using AzureProjectTest.Helper;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AzureProjectTest;

[GameClass(5), Timeout(Constants.TIMEOUT)]
internal class AppServiceTest
{
    private HttpClient HttpClient;
    private IAppServicePlan? appServicePlan;
    private IAppServiceManager? client;
    private IFunctionApp? functionApp;
    private StorageAccount? storageAccount;

    public AppServiceTest()
    {
        Setup();
    }

    [SetUp]
    public void Setup()
    {
        HttpClient = new();
        HttpClient.Timeout = TimeSpan.FromSeconds(85);

        var config = new Config();
        client = AppServiceManager.Configure().Authenticate(config.Credentials, config.SubscriptionId);
        appServicePlan = client.AppServicePlans.ListByResourceGroup(Constants.ResourceGroupName)
            .FirstOrDefault(c => c.Tags.ContainsKey("key") && c.Tags["key"] == "AppServicePlan");
        functionApp = client.FunctionApps.ListByResourceGroup(Constants.ResourceGroupName)
            .FirstOrDefault(c => c.Tags.ContainsKey("key") && c.Tags["key"] == "FunctionApp");

        var storageAccountTest = new StorageAccountTest();
        storageAccount = storageAccountTest.GetLogicStorageAccount(storageAccountTest!.GetStorageAccounts());

        storageAccountTest.TearDown();
    }

    [GameTask(
        "Can you a Azure Function App v4 in Hong Kong for node.js 16? I want to use Windows in Consumption plan. " +
        "Tag the AppServicePlan with {key:AppServicePlan}." +
        "Tag the FunctionApps with {key:FunctionApp}.",
    10, 20, 1)]
    [Test]
    public void Test01_AppServicePlanWithTag()
    {
        Assert.IsNotNull(appServicePlan, "AppService Plans with tag {key:AppServicePlan}.");
    }

    [GameTask(1)]
    [Test]
    public void Test02_FunctionAppsWithTag()
    {
        Assert.IsNotNull(functionApp, "Function App Plans with tag {key:FunctionApp}.");
    }

    [GameTask(1)]
    [Test]
    public void Test03_AppServicePlanSettings()
    {
        Assert.AreEqual("southeastasia", appServicePlan!.Region.Name);
        Assert.AreEqual("Dynamic", appServicePlan.PricingTier.SkuDescription.Tier);
        Assert.AreEqual("Y1", appServicePlan.PricingTier.SkuDescription.Size);
        Assert.AreEqual("Windows", appServicePlan.OperatingSystem.ToString());
    }

    [GameTask(
    "I want to set and confirm app settings:" +
        "1. WEBSITE_RUN_FROM_PACKAGE to Storage account with tag name 'usage' and value 'logic' URL i.e. https://{storageAccount.Name}.blob.core.windows.net/code/app.zip." +
        "2. StorageConnectionAppSetting to Storage account with tag name 'usage' and value 'logic' primary connect string." +
        "3. WEBSITE_CONTENTAZUREFILECONNECTIONSTRING to Storage account with tag name 'usage' and value 'logic' primary connect string.",
5, 20)]
    [Test]
    public void Test04_FunctionAppSettings()
    {
        Assert.AreEqual("southeastasia", functionApp!.Region.Name);
        IReadOnlyDictionary<string, IAppSetting> appSettings = functionApp.GetAppSettings();
        Assert.AreEqual("~4", appSettings["FUNCTIONS_EXTENSION_VERSION"].Value);
        Assert.AreEqual("node", appSettings["FUNCTIONS_WORKER_RUNTIME"].Value);
        Assert.AreEqual("~16", appSettings["WEBSITE_NODE_DEFAULT_VERSION"].Value);
        StringAssert.StartsWith($"https://{storageAccount!.Name}.blob.core.windows.net/code/",
            appSettings["WEBSITE_RUN_FROM_PACKAGE"].Value);
        StringAssert.EndsWith("app.zip", appSettings["WEBSITE_RUN_FROM_PACKAGE"].Value);
        StringAssert.StartsWith($"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey=",
            appSettings["StorageConnectionAppSetting"].Value);
        StringAssert.StartsWith($"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey=",
            appSettings["WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"].Value);
    }

    [GameTask(
"I want to set and confirm app settings:" +
     "APPINSIGHTS_INSTRUMENTATIONKEY to the ApplicationInsights InstrumentationKey.",
5, 10)]
    [Test]
    public void Test05_FunctionAppSettingsInstrumentationKey()
    {
        var applicationInsightTest = new ApplicationInsightTest();
        IReadOnlyDictionary<string, IAppSetting> appSettings = functionApp!.GetAppSettings();
        Assert.AreEqual(applicationInsightTest.GetApplicationInsights()!.InstrumentationKey,
            appSettings["APPINSIGHTS_INSTRUMENTATIONKEY"].Value);
    }

    [GameTask(
"Create a node.js Azure function with binding: " +
 "{\"disabled\":false,\"bindings\":[{\"type\":\"httpTrigger\",\"name\":\"req\",\"direction\":\"in\",\"dataType\":\"string\",\"authLevel\":\"anonymous\",\"methods\":[\"get\"]},{\"type\":\"http\",\"direction\":\"out\",\"name\":\"res\"},{\"type\":\"queue\",\"name\":\"jobQueue\",\"queueName\":\"job\",\"direction\":\"out\",\"connection\":\"StorageConnectionAppSetting\"},{\"tableName\":\"message\",\"name\":\"messageTable\",\"type\":\"table\",\"direction\":\"out\",\"connection\":\"StorageConnectionAppSetting\"}]}",
5, 10)]
    [Test]
    public void Test04_AzureFunctionBinding()
    {
        var helloFunction = functionApp!.ListFunctions()[0];
        const string functionJs = "{\"disabled\":false,\"bindings\":[{\"type\":\"httpTrigger\",\"name\":\"req\",\"direction\":\"in\",\"dataType\":\"string\",\"authLevel\":\"anonymous\",\"methods\":[\"get\"]},{\"type\":\"http\",\"direction\":\"out\",\"name\":\"res\"},{\"type\":\"queue\",\"name\":\"jobQueue\",\"queueName\":\"job\",\"direction\":\"out\",\"connection\":\"StorageConnectionAppSetting\"},{\"tableName\":\"message\",\"name\":\"messageTable\",\"type\":\"table\",\"direction\":\"out\",\"connection\":\"StorageConnectionAppSetting\"}]}";
        var configJsonString = JsonConvert.SerializeObject(helloFunction.Config);
        dynamic actual = JsonConvert.DeserializeObject(configJsonString);
        dynamic expected = JsonConvert.DeserializeObject(functionJs);
        Assert.AreEqual(expected, actual);
    }
    [GameTask(
        "Update a node.js Azure function source code " +
        "When receive a get request ?user=tester&message=abcd, then return 'Hello, tester and I received your message: abcd'",
10, 10)]
    [Test]
    public async Task Test05_AzureFunctionCallWithHttpResponse()
    {
        var helloFunction = functionApp!.ListFunctions()[0];
        var message = DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss");
        var url = helloFunction.Inner.InvokeUrlTemplate + "?user=tester&message=" + message;
        var helloResponse = await HttpClient.GetStringAsync(url);
        var expected = $@"Hello, tester and I received your message: {message}";
        Assert.AreEqual(expected, helloResponse);
    }

    [GameTask(
    "Update a node.js Azure function source code " +
    "When receive a get request ?user=tester&message=abcd, then save pk 'tester', row key 'abcd' into Azure Storage table named 'message'.",
10, 10)]
    [Test]
    public async Task Test06_AzureFunctionCallSaveDataToAzureTable()
    {
        var helloFunction = functionApp!.ListFunctions()[0];
        var message = DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss");
        var url = helloFunction.Inner.InvokeUrlTemplate + "?user=tester&message=" + message;
        await HttpClient.GetStringAsync(url);

        var appSettings = await functionApp!.GetAppSettingsAsync();
        var connectionString = appSettings["StorageConnectionAppSetting"].Value;

        var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
        var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
        var cloudTable = cloudTableClient.GetTableReference("message");

        //messageTable:[{ PartitionKey: user, RowKey: message, time: time}]
        var result = await cloudTable.ExecuteAsync(TableOperation.Retrieve("tester", message));
        Assert.IsNotNull(result.Result);
    }

    [GameTask(
"Update a node.js Azure function source code " +
"When receive a get request ?user=tester&message=abcd, then put message {'user':'tester','message': 'abcd','time':'<current time>'} into Azure Storage queue named 'job'.",
10, 10)]
    [Test]
    public async Task Test07_AzureFunctionCallPutMessageToQueue()
    {
        var helloFunction = functionApp!.ListFunctions()[0];

        var appSettings = await functionApp!.GetAppSettingsAsync();
        var connectionString = appSettings["StorageConnectionAppSetting"].Value;

        var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
        var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
        var queue = cloudQueueClient.GetQueueReference("job");
        await queue.ClearAsync();

        var message = DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss");
        var url = helloFunction.Inner.InvokeUrlTemplate + "?user=tester&message=" + message;
        await HttpClient.GetStringAsync(url);

        var messageAsync = queue.GetMessageAsync();

        Assert.IsNotNull(messageAsync.Result);
        var resultAsString = messageAsync.Result.AsString;

        StringAssert.Contains($"\"message\": \"{message}\"", resultAsString);
    }
}