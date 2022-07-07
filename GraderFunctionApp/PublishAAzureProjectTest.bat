cd ..\AzureProjectTest\
dotnet publish -p:PublishProfile=FolderProfile
cd .. 
xcopy AzureProjectTest\bin\Release\net6.0\publish\win-x64\AzureProjectTest.exe GraderFunctionApp\