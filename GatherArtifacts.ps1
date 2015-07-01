# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    # Restructure output
    cd Artifacts

    Write-Host ("Current directory: '" + (pwd).Path + "'")
    
    $folders = dir

    Write-Host ("Creating directory: 'PerJahnUtils'")
    #md PerJahnUtils
    New-Item 'PerJahnUtils' -Type Directory | Out-Null

    $folders | % {
        [string] $source = $_.Name + "\*"
        [string] $target = "PerJahnUtils"
        Write-Host ("Moving file: '" + $source + "' -> '" + $target + "'")
        move $source $target

        Write-Host ("Deleting directory: '" + $_.Name + "'")
        rd $_.Name
    }
}

Main
