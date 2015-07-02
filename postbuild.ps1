# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Move-PublicArtifacts
}

function Move-PublicArtifacts([string] $buildfile)
{
    [string] $source = "\\wipweb01.wipcore.se\downloads\PerJ\Users\Users_PerJ\PerJahnUtils.zip"
    [string] $target = "\\wipweb01.wipcore.se\downloads\PerJ\PerJahnUtils.zip"
    if (Test-Path $target)
    {
        del $target
    }
    move $source $target

    [string] $folder = Split-Path (Split-Path $source)
    Write-Host ("Removing folder: '" + $folder + "'")
    rd $folder -Recurse
}

Main
