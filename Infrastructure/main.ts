import { AzureApiManagementConstruct } from "azure-common-construct/patterns/AzureApiManagementConstruct";
import { AzureFunctionFileSharePublisherConstruct } from "azure-common-construct/patterns/AzureFunctionFileSharePublisherConstruct";
import { AzureFunctionWindowsConstruct } from "azure-common-construct/patterns/AzureFunctionWindowsConstruct";
import { PublishMode } from "azure-common-construct/patterns/PublisherConstruct";
import { App, TerraformOutput, TerraformStack } from "cdktf";
import { AzurermProvider, ResourceGroup } from "cdktf-azure-providers/.gen/providers/azurerm";
import { Resource } from "cdktf-azure-providers/.gen/providers/null";
import { Construct } from "constructs";
import path = require("path");

import * as dotenv from 'dotenv';
dotenv.config({ path: __dirname + '/.env' });

class AzureAutomaticGradingEngineGraderStack extends TerraformStack {
  constructor(scope: Construct, name: string) {
    super(scope, name);

    new AzurermProvider(this, "AzureRm", {
      features: {
        resourceGroup: {
          preventDeletionIfContainsResources: false
        }
      }
    })

    const prefix = "GradingEngineAssignment"
    const environment = "dev"

    const resourceGroup = new ResourceGroup(this, "ResourceGroup", {
      location: "EastAsia",
      name: prefix + "ResourceGroup"
    })

    const appSettings = {
    }

    const azureFunctionConstruct = new AzureFunctionWindowsConstruct(this, "AzureFunctionConstruct", {
      functionAppName: process.env.FUNCTION_APP_NAME!,
      environment,
      prefix,
      resourceGroup,
      appSettings,
      vsProjectPath: path.join(__dirname, "..", "GraderFunctionApp/"),
      publishMode: PublishMode.Always
    })
    azureFunctionConstruct.functionApp.siteConfig.cors.allowedOrigins = ["*"];

    const buildTestProjectResource = new Resource(this, "BuildFunctionAppResource",
      {
        triggers: { build_hash: "${timestamp()}" },
        dependsOn: [azureFunctionConstruct.functionApp]
      })

    buildTestProjectResource.addOverride("provisioner", [
      {
        "local-exec": {
          working_dir: path.join(__dirname, "..", "AzureProjectTest/"),
          command: "dotnet publish -p:PublishProfile=FolderProfile"
        },
      },
    ]);

    const testOutputFolder = path.join(__dirname, "..", "/AzureProjectTest/bin/Release/net6.0/publish/win-x64/");
    const azureFunctionFileSharePublisherConstruct = new AzureFunctionFileSharePublisherConstruct(this, "AzureFunctionFileSharePublisherConstruct", {
      functionApp: azureFunctionConstruct.functionApp,
      functionFolder: "Tests",
      localFolder: testOutputFolder,
      storageAccount: azureFunctionConstruct.storageAccount
    });
    azureFunctionFileSharePublisherConstruct.node.addDependency(buildTestProjectResource)

    const azureApiManagementConstruct = new AzureApiManagementConstruct(this, "AzureApiManagementConstruct", {
      apiName: process.env.API_NAME!,
      environment,
      prefix,
      functionApp: azureFunctionConstruct.functionApp,
      publisherEmail: process.env.PUBLISHER_EMAIL!,
      publisherName: process.env.PUBLISHER_NAME!,
      resourceGroup,
      skuName: "Basic_1",
      wpiUsers: [{ email: process.env.API_EMAIL!, firstName: "API", lastName: "API", id: "unique" }],
      functionNames: ["AzureGraderFunction"],
      ipRateLimit: 60,
      corsDomain: "*"
    })

    new TerraformOutput(this, "ApiManagementAzureGraderFunctionUrl", {
      value: `${azureApiManagementConstruct.apiManagement.gatewayUrl}/AzureGraderFunction`
    })
    new TerraformOutput(this, "ApiKey", {
      sensitive: true,
      value: azureApiManagementConstruct.apiUsers[0].apiKey
    })

    new TerraformOutput(this, "FunctionAppHostname", {
      value: azureFunctionConstruct.functionApp.name
    })
    new TerraformOutput(this, "AzureFunctionBaseUrl", {
      value: `https://${azureFunctionConstruct.functionApp.name}.azurewebsites.net`
    })
    new TerraformOutput(this, "AzureGraderFunctionUrl", {
      value: `https://${azureFunctionConstruct.functionApp.name}.azurewebsites.net/api/AzureGraderFunction`
    })
  }
}

const app = new App({ skipValidation: true });
new AzureAutomaticGradingEngineGraderStack(app, "AzureAutomaticGradingEngineGrader");
app.synth();
