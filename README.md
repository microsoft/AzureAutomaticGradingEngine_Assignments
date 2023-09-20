# Azure Automatic Grading Engine - Classroom Assignments samples

This repository contains sample classroom assignments for classroom activity for grading students submissions of Azure Services by the [Azure Automatic Grading Solution](http://github.com/microsoft/azureautomaticgradingengine)

## Example Student Classroom Assessment Tasks:

Students are asked to create the following Azure Infrastructure 

1. Create 2 Virtual Networks in 2 regions.
2. Create 2 Subnets in each Virtual Network.
3. Create Route Tables & Network Security Groups.
4. Create Virtual Network Peering for 2 Virtual Networks.
5. Create 2 Storage Accounts - one for an Azure Function and another for a Azure Static Website.
    1. Azure Function Storage Account contains 1 Storage Container, 1 Storage Queue, and 1 StorageTable.
    2. Static website Storage Account contains index page *index.html* and error page *error.html*.
6. Create 1 Application Insights with Log Analytics Workspace.
7. Create 1 Azure Function App with 1 Azure Function.

# Azure Project Grader for Automatic Grading Engine 

For course testing Microsoft Azure, it is hard to assess or grade Azure project manually. This project makes use of the technique of unit test to grade student's Azure project settings automatically to validate have the students created the above resources and services.

A rubic assessment is then completed on the task and students recieve grades based on the nunit outcomes and validation. 

This project has been developed by [Cyrus Wong]( https://www.linkedin.com/in/cyruswong) [Microsoft MVP Azure](https://mvp.microsoft.com/en-US/mvp/profile/86da86ff-8786-ed11-aad1-000d3a197333WT.mc_id=AZ-MVP-5005120) and [Microsoft Learn Educator Ambassador](https://docs.microsoft.com/learn/roles/educator/learn-for-educators-overview) in Association with the [Microsoft Next Generation Developer Relations Team](https://techcommunity.microsoft.com/t5/educator-developer-blog/bg-p/EducatorDeveloperBlog?WT.mc_id=academic-39457-leestott).

Project collaborators include, [Kwok,Hau Ling](https://www.linkedin.com/in/hau-ling-kwok-657b9624a/), [Lau Hing Pui](https://www.linkedin.com/in/leolaulhp/), and [Xu Yuan](https://www.linkedin.com/in/xu-yuan-flora/) from the IT114115 Higher Diploma in Cloud and Data Centre Administration.

The project is being validated through usage on the course [Higher Diploma in Cloud and Data Centre Administration](https://www.vtc.edu.hk/admission/en/programme/it114115-higher-diploma-in-cloud-and-data-centre-administration/)

## Testing the Sample Classroom Setup 

## CDK-TF Deployment 
You have to refer [Object Oriented Your Azure Infrastructure with Cloud Development Kit for Terraform (CDKTF)](https://techcommunity.microsoft.com/t5/educator-developer-blog/object-oriented-your-azure-infrastructure-with-cloud-development/ba-p/3474715) and setup your CDK-TF.

Update .env.template and rename it to .env
```
FUNCTION_APP_NAME=
AZURE_OPENAI_ENDPOINT=https://eastus.api.cognitive.microsoft.com/
AZURE_OPENAI_API_KEY=
DEPLOYMENT_OR_MODEL_NAME=gpt-35-turbo
```

```
npm i
cdktf deploy --auto-approve
```

## Package UnitTest into exe
Go to the \AzureProjectGrader\AzureProjectGrader path and run.
```
dotnet publish -r win-x64 -c Release
```
Use Microsoft Azure Storage Explorer 
Upload all files in ```\AzureProjectTest\bin\Release\net6.0\win-x64``` to the storage account file share ```data/Functions/Tests```.


## Contributing to Samples

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
