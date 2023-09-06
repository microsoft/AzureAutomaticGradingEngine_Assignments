using Azure.Identity;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using NUnit.Framework;

namespace AzureProjectTestLib.Helper;

public class Config
{
    public Config()
    {
        var azureAuthFilePath = TestContext.Parameters.Get("AzureCredentialsPath", null);
        var trace = TestContext.Parameters.Get("trace", null);
        TestContext.Out.WriteLine(trace);
        var appPrincipal = AppPrincipal.FromJson(File.ReadAllText(azureAuthFilePath!));
        Credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(appPrincipal.appId, appPrincipal.password,
            appPrincipal.tenant, AzureEnvironment.AzureGlobalCloud);
        var authenticated = Microsoft.Azure.Management.Fluent.Azure.Configure().Authenticate(Credentials);
        var subscriptionId = authenticated.Subscriptions.List().First<ISubscription>().SubscriptionId;
        SubscriptionId = subscriptionId;
        ClientSecretCredential =
            new ClientSecretCredential(appPrincipal.tenant, appPrincipal.appId, appPrincipal.password);
    }


    public ClientSecretCredential ClientSecretCredential { get; }
    public AzureCredentials Credentials { get; }
    public string SubscriptionId { get; }
}