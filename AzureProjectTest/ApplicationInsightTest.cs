using Azure.Core;
using AzureProjectTest.Helper;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using NUnit.Framework;

namespace AzureProjectTest;

[GameClass(4)]
[Parallelizable(ParallelScope.Children)]
internal class ApplicationInsightTest
{
    private ApplicationInsightsComponent? applicationInsight;
    private ApplicationInsightsManagementClient? client;


    public ApplicationInsightTest()
    {
        Setup();
    }

    public ApplicationInsightsComponent? GetApplicationInsights()
    {
        return client!.Components.List()
            .FirstOrDefault(c => c.Tags.ContainsKey("key") && c.Tags["key"] == "ApplicationInsights");
    }

    [SetUp]
    public void Setup()
    {
        var config = new Config();
        client = new ApplicationInsightsManagementClient(config.Credentials, new HttpClient(), true);
        client.SubscriptionId = config.SubscriptionId;
        applicationInsight = GetApplicationInsights();
    }

    [GameTask(
    "Can you a ApplicationInsight in Hong Kong? Type is 'other', keeps log for 30 days, and tag name is 'key' with value 'ApplicationInsights'.",    
    3, 10, 1)]
    [Test]
    public void Test01_AppServicePlanWithTag()
    {
        Assert.IsNotNull(applicationInsight, "Application Insights with tag {key:ApplicationInsights}.");
    }

    [GameTask(1)]
    [Test]
    public void Test02_AppServicePlanSettings()
    {
        Assert.AreEqual(AzureLocation.EastAsia.ToString(), applicationInsight!.Location);
        Assert.AreEqual("other", applicationInsight.ApplicationType);
        Assert.AreEqual(30, applicationInsight.RetentionInDays);
    }
}