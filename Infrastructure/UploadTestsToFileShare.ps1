[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $connectionString
)

$share = az storage share list --connection-string $connectionString | ConvertFrom-Json
$shareName = $share.name
Write-Output $shareName

az storage directory create --name "data/Functions/Tests" --share-name $shareName --connection-string $connectionString

Get-ChildItem "..\AzureProjectTest\bin\Release\net6.0\publish\win-x64"| 
Foreach-Object {    
    az storage file upload --connection-string $connectionString --share-name $shareName --path data/Functions/Tests/$_ --source $_.FullName
}
