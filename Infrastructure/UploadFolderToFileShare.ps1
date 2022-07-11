[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $connectionString,
    [Parameter(Mandatory = $true)]
    [string]
    $folder
)

$share = az storage share list --connection-string $connectionString | ConvertFrom-Json
$shareName = $share.name
Write-Output $shareName

az storage directory create --name "data/Functions/Tests" --share-name $shareName --connection-string $connectionString
Get-ChildItem $folder | 
Foreach-Object {    
    az storage file upload --share-name $shareName --path data/Functions/Tests/$_ --source $_.FullName --connection-string $connectionString
}
