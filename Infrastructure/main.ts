import { AzureFunctionFileSharePublisherConstruct } from "azure-common-construct/patterns/AzureFunctionFileSharePublisherConstruct";
import { AzureFunctionWindowsConstruct } from "azure-common-construct/patterns/AzureFunctionWindowsConstruct";
import { PublishMode } from "azure-common-construct/patterns/PublisherConstruct";
import { App, TerraformOutput, TerraformStack } from "cdktf";
import { AzurermProvider } from "cdktf-azure-providers/.gen/providers/azurerm/azurerm-provider";
import { ResourceGroup } from "cdktf-azure-providers/.gen/providers/azurerm/resource-group";

import { Resource } from "cdktf-azure-providers/.gen/providers/null/resource";
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

    const resourceGroup = new ResourceGroup(this, prefix + "ResourceGroup", {
      location: "EastAsia",
      name: prefix + "ResourceGroup"
    })

    const appSettings = {
      AZURE_OPENAI_ENDPOINT: process.env.AZURE_OPENAI_ENDPOINT!,
      AZURE_OPENAI_API_KEY: process.env.AZURE_OPENAI_API_KEY!,
      DEPLOYMENT_OR_MODEL_NAME: process.env.DEPLOYMENT_OR_MODEL_NAME!
    }

    const azureFunctionConstruct = new AzureFunctionWindowsConstruct(this, prefix + "AzureFunctionConstruct", {
      functionAppName: process.env.FUNCTION_APP_NAME!,
      environment,
      prefix,
      resourceGroup,
      appSettings,
      vsProjectPath: path.join(__dirname, "..", "GraderFunctionApp/"),
      publishMode: PublishMode.Always,
      functionNames: ["GraderFunction", "GameTaskFunction"]
    })
    azureFunctionConstruct.functionApp.siteConfig.cors.allowedOrigins = ["*"];

    const buildTestProjectResource = new Resource(this, prefix + "BuildFunctionAppResource",
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
    const azureFunctionFileSharePublisherConstruct = new AzureFunctionFileSharePublisherConstruct(this, prefix + "AzureFunctionFileSharePublisherConstruct", {
      functionApp: azureFunctionConstruct.functionApp,
      functionFolder: "Tests",
      localFolder: testOutputFolder,
      storageAccount: azureFunctionConstruct.storageAccount,
    });
    azureFunctionFileSharePublisherConstruct.node.addDependency(buildTestProjectResource)

    new TerraformOutput(this, prefix + "GraderFunctionUrl", {
      value: azureFunctionConstruct.functionUrls!["GraderFunction"]
    });
    new TerraformOutput(this, prefix + "GameTaskFunctionUrl", {
      value: azureFunctionConstruct.functionUrls!["GameTaskFunction"]
    });
  }
}

const app = new App({ skipValidation: true });
new AzureAutomaticGradingEngineGraderStack(app, "AzureAutomaticGradingEngineGrader");
app.synth();
