# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main([string[]] $mainargs)
{
    Import-Module .\Launch.dll
    Import-Module .\SyncGatherFileSystem.psm1
    Import-Module .\SyncPerformCopy.psm1

    if (!(Test-Path .\SyncFiles.exe))
    {
        throw "SyncFiles.exe (Release) must be compiled!"
    }

    if ($mainargs.Count -lt 4 -or $mainargs.Count -gt 6)
    {
        Write-Host ("Usage: powershell -file Sync.ps1 <source environment> <target environment> <source servertype> <target servertype> [maxsize mb] [brands]")
        Write-Host ("")
        Write-Host ("Example: powershell -file Sync.ps1 Production Develop file web")
        return
    }

    [string] $source = $mainargs[0]
    [string] $target = $mainargs[1]
    [string] $sourcetype = $mainargs[2]
    [string] $targettype = $mainargs[3]

    if ($mainargs.Count -ge 5)
    {
        [long] $maxsize = ([long]$mainargs[4])*1mb
    }
    else
    {
        [long] $maxsize = 20mb
    }

    if ($mainargs.Count -ge 6)
    {
        [string] $brands = $mainargs[5]
    }
    else
    {
        [string] $brands = $null
    }


    Write-Host ("Source: " + $source)
    Write-Host ("Target: " + $target)
    Write-Host ("Source servertype: " + $sourcetype)
    Write-Host ("Target servertype: " + $targettype)
    Write-Host ("Maxsize: " + $maxsize)
    Write-Host ("Brands: " + $brands)


    GetFolderContent $brands $maxsize $source $target $sourcetype $targettype


    $choices = [System.Management.Automation.Host.ChoiceDescription[]]("&Yes", "&No")
    $caption = ""
    $message = ("Start copying to " + $source + ":" + $sourcetype + " to " + $target + ":" + $targettype + "?")
    $result  = $host.ui.PromptForChoice($caption, $message, $choices, 1)
    if ($result)
    {
        exit 1
    }


    Write-Host -f Cyan ($source + ":" + $sourcetype + " -> " + $target + ":" + $targettype + " (" + $maxsize + ")")
    SyncFiles $brands $maxsize $source $target $sourcetype $targettype
}

Main $args
