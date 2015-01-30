# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function SyncFiles([string[]] $brands, [long] $maxsize, [string] $sourceenv, [string] $targetenv, [string] $sourcetype, [string] $targettype)
{
    Set-Alias SyncFilesExe .\SyncFiles.exe

    if (!$brands)
    {
        $brands = GetAllBrands $sourceenv $targetenv
    }

    $brands | % {
        [string] $brand = $_

        $syncsource  = Get-LaunchSyncFolder -env $sourceenv -brand $brand -servertype $sourcetype
        $synctargets = Get-LaunchSyncFolder -env $targetenv -brand $brand -servertype $targettype

        if (!$syncsource)
        {
            Write-Host -f Yellow ("Brand '" + $brand + "' has no sync folder specified in environment '" + $sourceenv + "' for servertype '" + $sourcetype + "'.")
        }
        if (!$synctargets)
        {
            Write-Host -f Yellow ("Brand '" + $brand + "' has no sync folder specified in environment '" + $targetenv + "' for servertype '" + $targettype + "'.")
        }
        if (!$syncsource -or !$synctargets)
        {
            return
        }

        [string] $sourceserver = $syncsource.Server
        [string] $sourcefile = "tree_" + $sourceserver.Split(".")[0] + ".txt"
        [string] $sourcepath = $syncsource.Share.Replace("[server]", $sourceserver)

        $synctargets | % {
            [string] $targetserver = $_.Server
            [string] $targetfile = "tree_" + $targetserver.Split(".")[0] + ".txt"
            [string] $targetpath = $_.Share.Replace("[server]", $targetserver)

            SyncFilesExe ("-m" + $maxsize) $sourcefile $targetfile $sourcepath $targetpath
        }
    }}

function GetAllBrands([string] $sourceenv, [string] $targetenv)
{
    [string[]] $sourcebrands = Get-LaunchBrand -env $sourceenv
    [string[]] $targetbrands = Get-LaunchBrand -env $targetenv

    [string[]] $brands = $sourcebrands | ? { $targetbrands -contains $_ }

    return $brands
}
