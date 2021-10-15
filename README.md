# Azure Automatic Grading Engine - Classroom Assignments samples

This repository contains sample classroom assignments for classroom activity for grading students submissions of Azure Services by the [Azure Automatic Grading Solution](http://github.com/microsoft/azureautomaticgradingengine)

# Azure Project Grader for Automatic Grading Engine 

For course testing Microsoft Azure, it is hard to assess or grade Azure project manually. This project makes use of the technique of unit test to grade student's Azure project settings automatically.

This project has been developed by [Cyrus Wong]( https://www.linkedin.com/in/cyruswong) [Microsoft Learn Educator Ambassador](https://docs.microsoft.com/learn/roles/educator/learn-for-educators-overview) in Association with the [Microsoft Next Generation Developer Relations Team](https://techcommunity.microsoft.com/t5/educator-developer-blog/bg-p/EducatorDeveloperBlog?WT.mc_id=academic-39457-leestott).
Project collaborators include, [Chan Yiu Leung](https://www.linkedin.com/in/hadeschan/), [So Ka Chun](https://www.linkedin.com/in/so-ka-chun-0643971a5/), [Lo Chun Hei](https://www.linkedin.com/in/chunhei-lo-86a9301b5/), [Ling Po Chu](https://www.linkedin.com/in/po-chu-ling-88392b1b5/), [Cheung Ho Shing](https://www.linkedin.com/in/cheunghoshing/) and [Pearly Law](https://www.linkedin.com/in/mei-ching-pearly-jean-law-172707171/) from the IT114115 Higher Diploma in Cloud and Data Centre Administration.

The project is being validated through usage on the course [Higher Diploma in Cloud and Data Centre Administration](https://www.vtc.edu.hk/admission/en/programme/it114115-higher-diploma-in-cloud-and-data-centre-administration/)

## Deployment

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmicrosoft%2FAzureAutomaticGradingEngine_Assignments%2Fmain%2Fazuredeploy.json)


## Package UnitTest into exe
Go to the \AzureProjectGrader\AzureProjectGrader path and run.
dotnet publish -r win-x64 -c Release

## Generate the Prebuilt package

Get the latest zip package
AzureAutomaticGradingEngine\AzureAutomaticGradingEngineFunctionApp\obj\Release\netcoreapp3.1\PubTmp 

## Contributing

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
