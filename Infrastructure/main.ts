import { Construct } from "constructs";
import { App, Fn, TerraformOutput, TerraformStack } from "cdktf";
import { AzurermProvider, ResourceGroup } from "cdktf-azure-providers/.gen/providers/azurerm";
import { Resource } from "cdktf-azure-providers/.gen/providers/null"

import { AzureFunctionWindowsConstruct } from "azure-common-construct/patterns/AzureFunctionWindowsConstruct";
import path = require("path");
import { PublishMode } from "azure-common-construct/patterns/PublisherConstruct";

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
      publishMode: PublishMode.AfterCodeChange
    })

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

    const uploadTestProjectResource = new Resource(this, "UploadTestResource",
      {
        triggers: { build_hash: "${timestamp()}" },
        dependsOn: [buildTestProjectResource]
      })
    const script = path.join(__dirname, "UploadFolderToFileShare.ps1");
    const testOutputFolder = path.join(__dirname, "..", "/AzureProjectTest/bin/Release/net6.0/publish/win-x64/");
    uploadTestProjectResource.addOverride("provisioner", [
      {
        "local-exec": {
          command: `powershell -ExecutionPolicy ByPass -File ${script} -connectionString \"${Fn.nonsensitive(azureFunctionConstruct.storageAccount.primaryConnectionString)} -folder ${testOutputFolder}`
        }
      },
    ]);

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
