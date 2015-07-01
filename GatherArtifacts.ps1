# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    # Restructure output
    cd Artifacts

    Write-Host ("Current directory: '" + (pwd).Path + "'")
    
    Write-Host ("Creating directory: 'PerJahnUtils'")
    #md PerJahnUtils
    New-Item 'PerJahnUtils' -Type Directory | Out-Null

    $files = dir -r
    $files | % {
        Write-Host ("Moving file: '" + $_.FullName + "' -> 'PerJahnUtils'")
        move $_.FullName PerJahnUtils
    }
}

Main
