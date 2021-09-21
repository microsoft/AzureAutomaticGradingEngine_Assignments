using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace AzureProjectGrader
{
    class Config
    {
        public Config()
        {
            var azureAuthFilePath = Environment.GetEnvironmentVariable("AzureAuthFilePath") ?? @"C:\Users\developer\Documents\azureauth.json";
            Credentials = SdkContext.AzureCredentialsFactory.FromFile(azureAuthFilePath);
            SubscriptionId = Credentials.DefaultSubscriptionId; 
        }
        public AzureCredentials Credentials { get; private set; }
        public string SubscriptionId { get; private set; }
    }
}
